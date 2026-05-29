using System.Security.Claims;
using WorkoutTracker.Server.DataTransferObjects.Requests.WorkoutPlan;
using WorkoutTracker.Server.DataTransferObjects.Responses.WorkoutPlan;
using WorkoutTracker.Server.Extensions;
using WorkoutTracker.Server.Models.Repositories;

namespace WorkoutTracker.Server.Handlers;

public static class WorkoutPlanHandler
{
    public static async Task<IResult> GetWorkoutPlans(
        ClaimsPrincipal user,
        WorkoutPlanRepository planRepository)
    {
        var userId = user.GetUserId();
        var plans = await planRepository.GetAllForUserAsync(userId);
        return Results.Ok(new GetWorkoutPlansResponse
        {
            plans = plans.Select(p => p.ToData()).ToList()
        });
    }

    public static async Task<IResult> AddWorkoutPlan(
        AddWorkoutPlanRequest request,
        ClaimsPrincipal user,
        WorkoutPlanRepository planRepository)
    {
        var userId = user.GetUserId();
        var planId = await planRepository.AddAsync(userId, request.name, request.days);
        return Results.Ok(planId);
    }

    public static async Task<IResult> UpdateWorkoutPlan(
        UpdateWorkoutPlanRequest request,
        ClaimsPrincipal user,
        WorkoutPlanRepository planRepository)
    {
        var userId = user.GetUserId();
        var plan = await planRepository.GetByIdAsync(request.planId);

        if (plan == null) return Results.NotFound();
        if (plan.UserId != userId) return Results.Forbid();

        var success = await planRepository.UpdateAsync(request.planId, request.name, request.days);
        return success ? Results.Ok() : Results.BadRequest();
    }

    public static async Task<IResult> DeleteWorkoutPlan(
        Guid planId,
        ClaimsPrincipal user,
        WorkoutPlanRepository planRepository)
    {
        var userId = user.GetUserId();
        var plan = await planRepository.GetByIdAsync(planId);

        if (plan == null) return Results.NotFound();
        if (plan.UserId != userId) return Results.Forbid();

        var success = await planRepository.DeleteAsync(planId);
        return success ? Results.Ok() : Results.BadRequest();
    }

    public static async Task<IResult> ActivateWorkoutPlan(
        Guid planId,
        ClaimsPrincipal user,
        WorkoutPlanRepository planRepository)
    {
        var userId = user.GetUserId();
        var plan = await planRepository.GetByIdAsync(planId);

        if (plan == null) return Results.NotFound();
        if (plan.UserId != userId) return Results.Forbid();

        await planRepository.ActivateAsync(userId, planId);
        return Results.Ok();
    }

    public static async Task<IResult> GetTodaysWorkout(
        ClaimsPrincipal user,
        WorkoutPlanRepository planRepository)
    {
        var userId = user.GetUserId();
        var dayOfWeek = (int)DateTime.UtcNow.DayOfWeek;
        var planDay = await planRepository.GetTodaysPlanDayAsync(userId, dayOfWeek);

        if (planDay == null)
        {
            return Results.Ok(new GetTodaysWorkoutResponse());
        }

        return Results.Ok(new GetTodaysWorkoutResponse
        {
            template = planDay.WorkoutTemplate?.ToData(),
            note = planDay.Note
        });
    }
}
