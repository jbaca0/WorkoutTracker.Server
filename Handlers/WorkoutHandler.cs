using System;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WorkoutTracker.Server.DataTransferObjects.Responses;
using WorkoutTracker.Server.DataTransferObjects.Responses.Workout;
using WorkoutTracker.Server.DataTransferObjects.Requests;
using WorkoutTracker.Server.Extensions;
using WorkoutTracker.Server.Models;
using WorkoutTracker.Server.Models.Repositories;
using WorkoutTracker.Server.Services;
using WorkoutTracker.Server.DomainObjects;


namespace WorkoutTracker.Server.Handlers;

public static class WorkoutHandler
{
    public static async Task<IResult> AddWorkout(HttpContext context, ApplicationDbContext dbContext)
    {
        var userId = context.User.GetUserId();

        var workout = new Models.Workout();
        workout.UserId = userId;
        dbContext.Workouts.Add(workout);
        await dbContext.SaveChangesAsync();
        return Results.Ok(workout.Id);
    }

    public static async Task<IResult> GetWorkouts(HttpContext context, ApplicationDbContext dbContext)
    {
        var userId = context.User.GetUserId();
        var workoutIds = await dbContext.Workouts
            .Where(w => w.UserId == userId)
            .Select(w => w.Id)
            .ToListAsync();
        return Results.Ok(workoutIds);
    }

    public static async Task<IResult> GetExercises(
        Guid workoutId,
        WorkoutRepository workoutRepository)
    {
        var exercises = await workoutRepository.GetExercisesAsync(workoutId);
        var exerciseNames = exercises
            .Select(e => e.ExerciseInfo.Name)
            .ToList();
        return Results.Ok(exerciseNames);
    }

    public static async Task<IResult> AddExercise(ApplicationDbContext dbContext, AddExerciseRequest request)
    {
        Models.Workout? workout = dbContext.Workouts.Find(request.workoutId);
        Models.ExerciseInfo? exerciseInfo = dbContext.ExerciseInfos.Find(request.exerciseInfoId);

        if (workout == null || exerciseInfo == null)
        {
            return Results.NotFound("Invalid workoutId or exerciseInfoId");
        }

        var exercise = new Models.Exercise
        {
            WorkoutId = workout.Id,
            ExerciseInfoId = exerciseInfo.Id
        };
        workout.Exercises.Add(exercise);
        await dbContext.SaveChangesAsync();
        return Results.Ok(new AddExerciseResponse { exerciseId = exercise.Id });
    }

    public static async Task<IResult> AddExercises(
        AddExercisesRequest request,
        ClaimsPrincipal user,
        WorkoutRepository workoutRepository,
        UserExerciseInfoService userExerciseInfoService)
    {
        if (request.exerciseInfoIds == null)
        {
            return Results.BadRequest();
        }

        bool success = await workoutRepository.AddExercises(request.workoutId, request.exerciseInfoIds);
        if (success == false)
        {
            return Results.BadRequest();
        }

        var domainUserExerciseInfos = await userExerciseInfoService.GetByIdsAsync(user.GetUserId(), request.exerciseInfoIds, request.workoutId);

        // Convert Domain objects to DTOs
        var dtoUserExerciseInfos = domainUserExerciseInfos.Select(domain => new DataTransferObjects.Common.UserExerciseInfo
        {
            exerciseInfo = new DataTransferObjects.Common.ExerciseInfo
            {
                id = domain.ExerciseInfo.Id,
                name = domain.ExerciseInfo.Name
            },
            latestExercise = domain.LatestExercise != null ? new DataTransferObjects.Common.Exercise
            {
                exerciseInfoId = domain.LatestExercise.ExerciseInfoId,
                sets = domain.LatestExercise.Sets.Select(s => new DataTransferObjects.Common.Set
                {
                    reps = s.Reps,
                    weight = s.Weight,
                    order = s.Order
                }).ToList()
            } : null,
            bestSetWeight = domain.BestSetWeight,
            bestSetReps = domain.BestSetReps,
            goalRecommendation = domain.GoalRecommendation
        }).ToList();

        var response = new AddExercisesResponse()
        {
            userExerciseInfos = dtoUserExerciseInfos
        };

        return Results.Ok(response);
    }

    public static async Task<IResult> Save(
        SaveRequest request,
        ClaimsPrincipal user,
        WorkoutRepository workoutRepository)
    {
        bool saveSuccessful = await workoutRepository.SaveAsync(DomainObjects.Workout.FromData(request.workout));
        if (saveSuccessful == false)
        {
            return Results.BadRequest();
        }
        return Results.Ok();
    }

    public static async Task<IResult> GetWorkoutHistory(
        HttpContext context,
        WorkoutRepository workoutRepository)
    {
        var userId = context.User.GetUserId();
        var workouts = await workoutRepository.GetWorkoutsWithSummaryAsync(userId);

        var summaries = workouts.Select(w => new DataTransferObjects.Common.WorkoutSummary
        {
            id = w.Id,
            addDateTime = w.AddDateTime,
            exerciseCount = w.Exercises.Count,
            exerciseNames = w.Exercises.Take(3).Select(e => e.ExerciseInfo.Name).ToList()
        }).ToList();

        return Results.Ok(new GetWorkoutHistoryResponse { workouts = summaries });
    }

    public static async Task<IResult> Cancel(
        Guid workoutId,
        WorkoutRepository workoutRepository)
    {
        bool success = await workoutRepository.DeleteAsync(workoutId);
        if (!success)
        {
            return Results.BadRequest();
        }
        return Results.Ok();
    }

    public static async Task<IResult> GetWorkoutDetail(
        Guid workoutId,
        ClaimsPrincipal user,
        WorkoutRepository workoutRepository,
        ExerciseRepository exerciseRepository,
        ExerciseGoalRepository exerciseGoalRepository,
        WorkoutPlanRepository workoutPlanRepository)
    {
        var workout = await workoutRepository.GetWorkoutWithDetailsAsync(workoutId);

        if (workout == null)
        {
            return Results.NotFound();
        }

        var userId = user.GetUserId();
        var exerciseInfoIds = workout.Exercises.Select(e => e.ExerciseInfoId).Distinct().ToList();
        var previousExercises = exerciseInfoIds.Count > 0
            ? await exerciseRepository.GetLatestExercisesForUserAsync(userId, exerciseInfoIds)
            : new List<DomainObjects.Exercise>();
        var previousByInfoId = previousExercises.ToDictionary(e => e.ExerciseInfoId);

        var goals = exerciseInfoIds.Count > 0
            ? await exerciseGoalRepository.GetByExerciseInfoIdsAsync(userId, exerciseInfoIds)
            : new List<DomainObjects.ExerciseGoal>();
        var goalsByInfoId = goals.ToDictionary(g => g.ExerciseInfoId);

        var planCache = new Dictionary<Guid, DomainObjects.WorkoutPlan?>();
        var recommendationsByInfoId = new Dictionary<Guid, DataTransferObjects.Common.GoalRecommendation>();
        foreach (var goal in goals)
        {
            if (!planCache.TryGetValue(goal.WorkoutPlanId, out var plan))
            {
                plan = await workoutPlanRepository.GetByIdWithDetailsAsync(goal.WorkoutPlanId);
                planCache[goal.WorkoutPlanId] = plan;
            }

            if (plan != null)
            {
                recommendationsByInfoId[goal.ExerciseInfoId] = goal.CalculateRecommendation(DateTime.UtcNow, plan);
            }
        }

        var workoutDto = new DataTransferObjects.Common.Workout
        {
            id = workout.Id,
            addDateTime = workout.AddDateTime,
            exercises = workout.Exercises.Select(e =>
            {
                var exerciseDto = new DataTransferObjects.Common.Exercise
                {
                    exerciseInfoId = e.ExerciseInfoId,
                    exerciseInfo = new DataTransferObjects.Common.ExerciseInfo
                    {
                        id = e.ExerciseInfo.Id,
                        name = e.ExerciseInfo.Name
                    },
                    sets = e.Sets.Select(s => new DataTransferObjects.Common.Set
                    {
                        id = s.Id,
                        reps = s.Reps,
                        weight = s.Weight,
                        completed = s.Completed,
                        order = s.Order
                    }).ToList()
                };

                if (previousByInfoId.TryGetValue(e.ExerciseInfoId, out var prev))
                {
                    exerciseDto.previousExercise = new DataTransferObjects.Common.Exercise
                    {
                        exerciseInfoId = prev.ExerciseInfoId,
                        sets = prev.Sets.Select(s => new DataTransferObjects.Common.Set
                        {
                            reps = s.Reps,
                            weight = s.Weight,
                            order = s.Order
                        }).ToList()
                    };
                }

                if (recommendationsByInfoId.TryGetValue(e.ExerciseInfoId, out var recommendation))
                {
                    exerciseDto.goalRecommendation = recommendation;
                }

                return exerciseDto;
            }).ToList()
        };

        return Results.Ok(new GetWorkoutDetailResponse { workout = workoutDto });
    }
}
