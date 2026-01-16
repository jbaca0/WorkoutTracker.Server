using System;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using WorkoutTracker.Server.Authorization.Requirements;
using WorkoutTracker.Server.Extensions;
using WorkoutTracker.Server.Models;

namespace WorkoutTracker.Server.Authorization.Handlers;

public class WorkoutOwnerHandler : AuthorizationHandler<WorkoutOwnerRequirement, Workout>
{
    private readonly ApplicationDbContext dbContext;

    public WorkoutOwnerHandler(ApplicationDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        WorkoutOwnerRequirement requirement,
        Workout workout)
    {
        bool validUserId = context.User.TryGetUserId(out Guid userId);
        if (workout == null
            || validUserId == false)
        {
            return;
        }

        if (workout.UserId == userId)
        {
            context.Succeed(requirement);
        }

        // No further action here; authorization is not granted by default.
        return;
    }
}


