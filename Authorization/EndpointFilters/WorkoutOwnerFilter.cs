using System;
using Microsoft.AspNetCore.Authorization;
using WorkoutTracker.Server.DataTransferObjects.Requests;
using WorkoutTracker.Server.Models.Repositories;

namespace WorkoutTracker.Server.Authorization.EndpointFilters;

public class WorkoutOwnerFilter : IEndpointFilter
{

    private readonly IAuthorizationService authorizationService;
    private readonly WorkoutRepository workoutRepository;

    public WorkoutOwnerFilter(IAuthorizationService authorizationService, WorkoutRepository workoutRepository)
    {
        this.authorizationService = authorizationService;
        this.workoutRepository = workoutRepository;
    }

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var user = context.HttpContext.User;
        if (!user.Identity?.IsAuthenticated ?? false)
            return Results.Unauthorized();

        Guid? workoutId = null;

        if (context.HttpContext.Request.RouteValues.TryGetValue("workoutId", out var routeValue) &&
            Guid.TryParse(routeValue?.ToString(), out var routeWorkoutId))
        {
            workoutId = routeWorkoutId;
        }

        if (workoutId == null)
        {
            var requestArg = context.Arguments.FirstOrDefault(a => a is IWorkoutRequest) as IWorkoutRequest;
            workoutId = requestArg?.workoutId;
        }

        if (workoutId == null)
            return Results.BadRequest("Missing workoutId");

        var workout = await workoutRepository.GetByIdAsync(workoutId.Value);
        if (workout == null)
            return Results.NotFound();

        var result = await authorizationService.AuthorizeAsync(user, workout, "WorkoutOwner");
        if (!result.Succeeded)
            return Results.Forbid();

        return await next(context);
    }
}
