using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using WorkoutTracker.Server.Authorization.Handlers;
using WorkoutTracker.Server.Authorization.Requirements;
using WorkoutTracker.Server.Tests.Helpers;
using Xunit;

namespace WorkoutTracker.Server.Tests.Authorization;

public class WorkoutOwnerHandlerTests
{
    private static async Task<AuthorizationHandlerContext> HandleAsync(
        System.Security.Claims.ClaimsPrincipal user,
        Models.Workout workout)
    {
        var requirement = new WorkoutOwnerRequirement();
        var context = new AuthorizationHandlerContext(new[] { requirement }, user, workout);
        var handler = new WorkoutOwnerHandler(DbContextFactory.CreateInMemory());
        await handler.HandleAsync(context);
        return context;
    }

    [Fact]
    public async Task HandleRequirementAsync_WhenUserOwnsWorkout_Succeeds()
    {
        var userId = Guid.NewGuid();
        var workout = new Models.Workout { Id = Guid.NewGuid(), UserId = userId };

        var context = await HandleAsync(ClaimsPrincipalBuilder.WithUserId(userId), workout);

        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task HandleRequirementAsync_WhenWorkoutBelongsToAnotherUser_DoesNotSucceed()
    {
        var workout = new Models.Workout { Id = Guid.NewGuid(), UserId = Guid.NewGuid() };

        var context = await HandleAsync(ClaimsPrincipalBuilder.WithUserId(Guid.NewGuid()), workout);

        context.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task HandleRequirementAsync_WhenUserHasNoUserIdClaim_DoesNotSucceed()
    {
        var workout = new Models.Workout { Id = Guid.NewGuid(), UserId = Guid.NewGuid() };

        var context = await HandleAsync(ClaimsPrincipalBuilder.WithNoUserId(), workout);

        context.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task HandleRequirementAsync_WhenUserIdClaimIsNotAGuid_DoesNotSucceed()
    {
        var workout = new Models.Workout { Id = Guid.NewGuid(), UserId = Guid.NewGuid() };

        var context = await HandleAsync(ClaimsPrincipalBuilder.WithInvalidUserId(), workout);

        context.HasSucceeded.Should().BeFalse();
    }
}
