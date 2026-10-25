using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using NSubstitute;
using WorkoutTracker.Server.Authorization.EndpointFilters;
using WorkoutTracker.Server.DataTransferObjects.Requests;
using WorkoutTracker.Server.Models.Repositories;
using WorkoutTracker.Server.Tests.Helpers;
using Xunit;

namespace WorkoutTracker.Server.Tests.Authorization;

public class WorkoutOwnerFilterTests
{
    private static readonly object NextResult = new();

    private static EndpointFilterDelegate Next(out List<EndpointFilterInvocationContext> invocations)
    {
        var captured = new List<EndpointFilterInvocationContext>();
        invocations = captured;
        return ctx =>
        {
            captured.Add(ctx);
            return ValueTask.FromResult<object?>(NextResult);
        };
    }

    private static IAuthorizationService AuthServiceReturning(bool succeeded)
    {
        var authService = Substitute.For<IAuthorizationService>();
        authService
            .AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object?>(), "WorkoutOwner")
            .Returns(succeeded ? AuthorizationResult.Success() : AuthorizationResult.Failed());
        return authService;
    }

    private static DefaultEndpointFilterInvocationContext MakeContext(
        ClaimsPrincipal user,
        Guid? routeWorkoutId = null,
        params object[] arguments)
    {
        var httpContext = new DefaultHttpContext { User = user };
        if (routeWorkoutId != null)
        {
            httpContext.Request.RouteValues["workoutId"] = routeWorkoutId.Value.ToString();
        }
        return new DefaultEndpointFilterInvocationContext(httpContext, arguments);
    }

    [Fact]
    public async Task InvokeAsync_WithoutWorkoutIdInRouteOrArguments_ReturnsBadRequest()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var filter = new WorkoutOwnerFilter(AuthServiceReturning(true), new WorkoutRepository(dbContext));
        var context = MakeContext(ClaimsPrincipalBuilder.WithUserId(Guid.NewGuid()));

        var result = await filter.InvokeAsync(context, Next(out _));

        result.Should().BeOfType<BadRequest<string>>();
    }

    [Fact]
    public async Task InvokeAsync_WhenWorkoutDoesNotExist_ReturnsNotFound()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var filter = new WorkoutOwnerFilter(AuthServiceReturning(true), new WorkoutRepository(dbContext));
        var context = MakeContext(ClaimsPrincipalBuilder.WithUserId(Guid.NewGuid()), routeWorkoutId: Guid.NewGuid());

        var result = await filter.InvokeAsync(context, Next(out _));

        result.Should().BeOfType<NotFound>();
    }

    [Fact]
    public async Task InvokeAsync_WhenAuthorizationFails_ReturnsForbid()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var workout = new Models.Workout { Id = Guid.NewGuid(), UserId = Guid.NewGuid() };
        dbContext.Workouts.Add(workout);
        await dbContext.SaveChangesAsync();

        var filter = new WorkoutOwnerFilter(AuthServiceReturning(false), new WorkoutRepository(dbContext));
        var context = MakeContext(ClaimsPrincipalBuilder.WithUserId(Guid.NewGuid()), routeWorkoutId: workout.Id);

        var result = await filter.InvokeAsync(context, Next(out var invocations));

        result.Should().BeOfType<ForbidHttpResult>();
        invocations.Should().BeEmpty();
    }

    [Fact]
    public async Task InvokeAsync_WithWorkoutIdFromRoute_WhenAuthorized_CallsNext()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var workout = new Models.Workout { Id = Guid.NewGuid(), UserId = Guid.NewGuid() };
        dbContext.Workouts.Add(workout);
        await dbContext.SaveChangesAsync();

        var filter = new WorkoutOwnerFilter(AuthServiceReturning(true), new WorkoutRepository(dbContext));
        var context = MakeContext(ClaimsPrincipalBuilder.WithUserId(workout.UserId), routeWorkoutId: workout.Id);

        var result = await filter.InvokeAsync(context, Next(out var invocations));

        result.Should().BeSameAs(NextResult);
        invocations.Should().ContainSingle();
    }

    [Fact]
    public async Task InvokeAsync_WithWorkoutIdFromRequestArgument_WhenAuthorized_CallsNext()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var workout = new Models.Workout { Id = Guid.NewGuid(), UserId = Guid.NewGuid() };
        dbContext.Workouts.Add(workout);
        await dbContext.SaveChangesAsync();

        var request = new AddExercisesRequest { workoutId = workout.Id, exerciseInfoIds = new List<Guid>() };
        var filter = new WorkoutOwnerFilter(AuthServiceReturning(true), new WorkoutRepository(dbContext));
        var context = MakeContext(ClaimsPrincipalBuilder.WithUserId(workout.UserId), routeWorkoutId: null, request);

        var result = await filter.InvokeAsync(context, Next(out var invocations));

        result.Should().BeSameAs(NextResult);
        invocations.Should().ContainSingle();
    }
}
