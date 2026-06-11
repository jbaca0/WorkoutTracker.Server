using System.Security.Claims;
using WorkoutTracker.Server.DataTransferObjects.Requests.ExerciseGoal;
using WorkoutTracker.Server.DataTransferObjects.Responses.ExerciseGoal;
using WorkoutTracker.Server.Extensions;
using WorkoutTracker.Server.Models.Repositories;

namespace WorkoutTracker.Server.Handlers;

public static class ExerciseGoalHandler
{
    public static async Task<IResult> GetGoals(
        ClaimsPrincipal user,
        ExerciseGoalRepository goalRepository,
        WorkoutPlanRepository planRepository)
    {
        var userId = user.GetUserId();
        var goals = await goalRepository.GetByUserIdAsync(userId);

        var dtos = new List<DataTransferObjects.Common.ExerciseGoal>();
        foreach (var goal in goals)
        {
            var plan = await planRepository.GetByIdWithDetailsAsync(goal.WorkoutPlanId);
            var recommendation = plan != null ? goal.CalculateRecommendation(DateTime.UtcNow, plan) : null;
            dtos.Add(goal.ToData(recommendation));
        }

        return Results.Ok(new GetExerciseGoalsResponse { goals = dtos });
    }

    public static async Task<IResult> CreateGoal(
        CreateExerciseGoalRequest request,
        ClaimsPrincipal user,
        ExerciseGoalRepository goalRepository,
        WorkoutPlanRepository planRepository,
        ExerciseRepository exerciseRepository)
    {
        var userId = user.GetUserId();

        var plan = await planRepository.GetByIdAsync(request.workoutPlanId);
        if (plan == null) return Results.NotFound();
        if (plan.UserId != userId) return Results.Forbid();

        decimal startWeight = request.startWeight ?? 0;
        int startReps = request.startReps ?? 0;

        if (request.startWeight == null || request.startReps == null)
        {
            var bestSet = await exerciseRepository.GetBestSetByEpleyAsync(userId, request.exerciseInfoId);
            if (bestSet != null)
            {
                startWeight = request.startWeight ?? bestSet.Value.weight;
                startReps = request.startReps ?? bestSet.Value.reps;
            }
        }

        var goal = await goalRepository.CreateAsync(
            userId,
            request.exerciseInfoId,
            request.workoutPlanId,
            startWeight,
            startReps,
            request.targetWeight,
            request.targetReps,
            request.goalDate);

        var planWithDetails = await planRepository.GetByIdWithDetailsAsync(request.workoutPlanId);
        var recommendation = planWithDetails != null ? goal.CalculateRecommendation(DateTime.UtcNow, planWithDetails) : null;

        return Results.Ok(goal.ToData(recommendation));
    }

    public static async Task<IResult> UpdateGoal(
        Guid id,
        UpdateExerciseGoalRequest request,
        ClaimsPrincipal user,
        ExerciseGoalRepository goalRepository)
    {
        var userId = user.GetUserId();

        var success = await goalRepository.UpdateAsync(
            id,
            userId,
            request.startWeight,
            request.startReps,
            request.targetWeight,
            request.targetReps,
            request.goalDate);

        return success ? Results.Ok() : Results.NotFound();
    }

    public static async Task<IResult> DeleteGoal(
        Guid id,
        ClaimsPrincipal user,
        ExerciseGoalRepository goalRepository)
    {
        var userId = user.GetUserId();
        var success = await goalRepository.DeleteAsync(id, userId);
        return success ? Results.Ok() : Results.NotFound();
    }
}
