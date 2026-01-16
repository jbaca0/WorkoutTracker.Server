using System;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WorkoutTracker.Server.DataTransferObjects.Responses;
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

        var domainUserExerciseInfos = await userExerciseInfoService.GetByIdsAsync(user.GetUserId(), request.exerciseInfoIds);

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
                    weight = s.Weight
                }).ToList()
            } : null
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
}
