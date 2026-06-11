using System.Security.Claims;
using WorkoutTracker.Server.DataTransferObjects.Requests.WorkoutTemplate;
using WorkoutTracker.Server.DataTransferObjects.Responses.WorkoutTemplate;
using WorkoutTracker.Server.Extensions;
using WorkoutTracker.Server.Models;
using WorkoutTracker.Server.Models.Repositories;
using WorkoutTracker.Server.Services;

namespace WorkoutTracker.Server.Handlers;

public static class WorkoutTemplateHandler
{
    public static async Task<IResult> GetTemplates(
        ClaimsPrincipal user,
        WorkoutTemplateRepository templateRepository)
    {
        var userId = user.GetUserId();
        var templates = await templateRepository.GetAllForUserAsync(userId);
        return Results.Ok(new GetTemplatesResponse
        {
            templates = templates.Select(t => t.ToData()).ToList()
        });
    }

    public static async Task<IResult> AddTemplate(
        CreateTemplateRequest request,
        ClaimsPrincipal user,
        WorkoutTemplateRepository templateRepository)
    {
        var userId = user.GetUserId();
        var templateId = await templateRepository.AddAsync(userId, request.name, request.exercises);
        return Results.Ok(templateId);
    }

    public static async Task<IResult> UpdateTemplate(
        UpdateTemplateRequest request,
        ClaimsPrincipal user,
        WorkoutTemplateRepository templateRepository)
    {
        var userId = user.GetUserId();
        var template = await templateRepository.GetByIdAsync(request.templateId);

        if (template == null) return Results.NotFound();
        if (template.UserId != userId) return Results.Forbid();

        var success = await templateRepository.UpdateAsync(request.templateId, request.name, request.exercises);
        return success ? Results.Ok() : Results.BadRequest();
    }

    public static async Task<IResult> DeleteTemplate(
        Guid templateId,
        ClaimsPrincipal user,
        WorkoutTemplateRepository templateRepository)
    {
        var userId = user.GetUserId();
        var template = await templateRepository.GetByIdAsync(templateId);

        if (template == null) return Results.NotFound();
        if (template.UserId != userId) return Results.Forbid();

        var success = await templateRepository.DeleteAsync(templateId);
        return success ? Results.Ok() : Results.BadRequest();
    }

    public static async Task<IResult> StartWorkoutFromTemplate(
        StartWorkoutFromTemplateRequest request,
        ClaimsPrincipal user,
        WorkoutTemplateRepository templateRepository,
        ApplicationDbContext dbContext,
        UserExerciseInfoService userExerciseInfoService)
    {
        var userId = user.GetUserId();
        var template = await templateRepository.GetByIdAsync(request.templateId);

        if (template == null) return Results.NotFound();
        if (template.UserId != userId) return Results.Forbid();

        var workout = new Models.Workout { UserId = userId };
        dbContext.Workouts.Add(workout);

        var exercises = template.Exercises
            .OrderBy(e => e.Order)
            .Select(e => new Models.Exercise
            {
                WorkoutId = workout.Id,
                ExerciseInfoId = e.ExerciseInfoId
            }).ToList();

        dbContext.Exercises.AddRange(exercises);
        await dbContext.SaveChangesAsync();

        var exerciseInfoIds = exercises.Select(e => e.ExerciseInfoId).ToList();
        var userExerciseInfos = await userExerciseInfoService.GetByIdsAsync(userId, exerciseInfoIds, workout.Id);

        var dtoUserExerciseInfos = userExerciseInfos.Select(domain => new DataTransferObjects.Common.UserExerciseInfo
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
            } : null,
            bestSetWeight = domain.BestSetWeight,
            bestSetReps = domain.BestSetReps,
            goalRecommendation = domain.GoalRecommendation
        }).ToList();

        return Results.Ok(new StartWorkoutFromTemplateResponse
        {
            workoutId = workout.Id,
            userExerciseInfos = dtoUserExerciseInfos
        });
    }
}
