using FluentAssertions;
using Microsoft.AspNetCore.Http.HttpResults;
using WorkoutTracker.Server.DataTransferObjects.Common;
using WorkoutTracker.Server.DataTransferObjects.Requests.ExerciseGoal;
using WorkoutTracker.Server.DataTransferObjects.Responses.ExerciseGoal;
using WorkoutTracker.Server.Handlers;
using WorkoutTracker.Server.Models.Repositories;
using WorkoutTracker.Server.Tests.Helpers;
using Xunit;

namespace WorkoutTracker.Server.Tests.Handlers;

public class ExerciseGoalHandlerTests
{
    private static Models.ExerciseInfo MakeExerciseInfo(string name = "Bench Press") =>
        new() { Id = Guid.NewGuid(), Name = name, SecondaryMuscleGroupsJson = "[]" };

    private static Models.WorkoutPlan MakePlan(Guid userId, string name = "Plan") =>
        new()
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = name,
            IsActive = true,
            StartDate = new DateTime(2026, 1, 1),
            CreatedDateTime = new DateTime(2026, 1, 1)
        };

    /// <summary>
    /// A goal whose start and target are the same estimated 1RM intensity, so the
    /// recommendation is the target weight itself regardless of what day it is run on.
    /// </summary>
    private static Models.ExerciseGoal MakeGoal(Guid userId, Guid exerciseInfoId, Guid? planId = null) =>
        new()
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ExerciseInfoId = exerciseInfoId,
            WorkoutPlanId = planId,
            StartWeight = 100m,
            StartReps = 5,
            TargetWeight = 140m,
            TargetReps = 5,
            GoalDate = new DateTime(2026, 6, 1),
            CreatedAt = new DateTime(2026, 1, 1)
        };

    private static async Task SeedHistoryAsync(
        Models.ApplicationDbContext dbContext, Guid userId, Guid exerciseInfoId, decimal weight, int reps)
    {
        var workout = new Models.Workout { Id = Guid.NewGuid(), UserId = userId, AddDateTime = DateTime.UtcNow.AddDays(-1) };
        var exercise = new Models.Exercise
        {
            Id = Guid.NewGuid(),
            WorkoutId = workout.Id,
            ExerciseInfoId = exerciseInfoId
        };
        exercise.Sets.Add(new Models.Set
        {
            Id = Guid.NewGuid(),
            ExerciseId = exercise.Id,
            Reps = reps,
            Weight = weight,
            Order = 1
        });
        dbContext.Workouts.Add(workout);
        dbContext.Exercises.Add(exercise);
        await dbContext.SaveChangesAsync();
    }

    // --- GetGoals ---

    [Fact]
    public async Task GetGoals_ReturnsOnlyTheCurrentUsersGoals()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var userId = Guid.NewGuid();
        var mine = MakeExerciseInfo("Squat");
        var theirs = MakeExerciseInfo("Row");
        dbContext.ExerciseInfos.AddRange(mine, theirs);
        dbContext.ExerciseGoals.AddRange(MakeGoal(userId, mine.Id), MakeGoal(Guid.NewGuid(), theirs.Id));
        await dbContext.SaveChangesAsync();

        var result = await ExerciseGoalHandler.GetGoals(
            ClaimsPrincipalBuilder.WithUserId(userId),
            new ExerciseGoalRepository(dbContext),
            new WorkoutPlanRepository(dbContext));

        var ok = result.Should().BeOfType<Ok<GetExerciseGoalsResponse>>().Subject;
        ok.Value!.goals.Should().ContainSingle().Which.exerciseInfo!.name.Should().Be("Squat");
    }

    [Fact]
    public async Task GetGoals_WithNoGoals_ReturnsAnEmptyList()
    {
        var dbContext = DbContextFactory.CreateInMemory();

        var result = await ExerciseGoalHandler.GetGoals(
            ClaimsPrincipalBuilder.WithUserId(Guid.NewGuid()),
            new ExerciseGoalRepository(dbContext),
            new WorkoutPlanRepository(dbContext));

        var ok = result.Should().BeOfType<Ok<GetExerciseGoalsResponse>>().Subject;
        ok.Value!.goals.Should().BeEmpty();
    }

    [Fact]
    public async Task GetGoals_ForAGoalWithNoPlan_OmitsTheRecommendation()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var userId = Guid.NewGuid();
        var exerciseInfo = MakeExerciseInfo();
        dbContext.ExerciseInfos.Add(exerciseInfo);
        dbContext.ExerciseGoals.Add(MakeGoal(userId, exerciseInfo.Id));
        await dbContext.SaveChangesAsync();

        var result = await ExerciseGoalHandler.GetGoals(
            ClaimsPrincipalBuilder.WithUserId(userId),
            new ExerciseGoalRepository(dbContext),
            new WorkoutPlanRepository(dbContext));

        var ok = result.Should().BeOfType<Ok<GetExerciseGoalsResponse>>().Subject;
        ok.Value!.goals.Should().ContainSingle().Which.goalRecommendation.Should().BeNull();
    }

    [Fact]
    public async Task GetGoals_ForAGoalOnAPlanWithNoSessionsForTheExercise_RecommendsTheTargetIntensity()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var userId = Guid.NewGuid();
        var exerciseInfo = MakeExerciseInfo();
        var plan = MakePlan(userId);
        dbContext.ExerciseInfos.Add(exerciseInfo);
        dbContext.WorkoutPlans.Add(plan);
        dbContext.ExerciseGoals.Add(MakeGoal(userId, exerciseInfo.Id, plan.Id));
        await dbContext.SaveChangesAsync();

        var result = await ExerciseGoalHandler.GetGoals(
            ClaimsPrincipalBuilder.WithUserId(userId),
            new ExerciseGoalRepository(dbContext),
            new WorkoutPlanRepository(dbContext));

        var ok = result.Should().BeOfType<Ok<GetExerciseGoalsResponse>>().Subject;
        var recommendation = ok.Value!.goals.Should().ContainSingle().Subject.goalRecommendation;
        recommendation.Should().NotBeNull();
        // The plan schedules no sessions for this exercise, so there is nothing to
        // interpolate across and the recommendation lands on the goal's own target.
        recommendation!.weight.Should().Be(140m);
        recommendation.reps.Should().Be(5);
    }

    [Fact]
    public async Task GetGoals_ForAGoalPointingAtAMissingPlan_OmitsTheRecommendation()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var userId = Guid.NewGuid();
        var exerciseInfo = MakeExerciseInfo();
        dbContext.ExerciseInfos.Add(exerciseInfo);
        dbContext.ExerciseGoals.Add(MakeGoal(userId, exerciseInfo.Id, Guid.NewGuid()));
        await dbContext.SaveChangesAsync();

        var result = await ExerciseGoalHandler.GetGoals(
            ClaimsPrincipalBuilder.WithUserId(userId),
            new ExerciseGoalRepository(dbContext),
            new WorkoutPlanRepository(dbContext));

        var ok = result.Should().BeOfType<Ok<GetExerciseGoalsResponse>>().Subject;
        ok.Value!.goals.Should().ContainSingle().Which.goalRecommendation.Should().BeNull();
    }

    // --- GetGoalsByPlan ---

    [Fact]
    public async Task GetGoalsByPlan_WithUnknownPlan_ReturnsNotFound()
    {
        var dbContext = DbContextFactory.CreateInMemory();

        var result = await ExerciseGoalHandler.GetGoalsByPlan(
            Guid.NewGuid(),
            ClaimsPrincipalBuilder.WithUserId(Guid.NewGuid()),
            new ExerciseGoalRepository(dbContext),
            new WorkoutPlanRepository(dbContext));

        result.Should().BeOfType<NotFound>();
    }

    [Fact]
    public async Task GetGoalsByPlan_WithAnotherUsersPlan_ReturnsForbid()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var plan = MakePlan(Guid.NewGuid());
        dbContext.WorkoutPlans.Add(plan);
        await dbContext.SaveChangesAsync();

        var result = await ExerciseGoalHandler.GetGoalsByPlan(
            plan.Id,
            ClaimsPrincipalBuilder.WithUserId(Guid.NewGuid()),
            new ExerciseGoalRepository(dbContext),
            new WorkoutPlanRepository(dbContext));

        result.Should().BeOfType<ForbidHttpResult>();
    }

    [Fact]
    public async Task GetGoalsByPlan_ReturnsOnlyTheGoalsAssignedToThatPlan()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var userId = Guid.NewGuid();
        var plan = MakePlan(userId);
        var onPlan = MakeExerciseInfo("On plan");
        var offPlan = MakeExerciseInfo("Off plan");
        dbContext.WorkoutPlans.Add(plan);
        dbContext.ExerciseInfos.AddRange(onPlan, offPlan);
        dbContext.ExerciseGoals.AddRange(
            MakeGoal(userId, onPlan.Id, plan.Id),
            MakeGoal(userId, offPlan.Id));
        await dbContext.SaveChangesAsync();

        var result = await ExerciseGoalHandler.GetGoalsByPlan(
            plan.Id,
            ClaimsPrincipalBuilder.WithUserId(userId),
            new ExerciseGoalRepository(dbContext),
            new WorkoutPlanRepository(dbContext));

        var ok = result.Should().BeOfType<Ok<GetExerciseGoalsResponse>>().Subject;
        var goal = ok.Value!.goals.Should().ContainSingle().Subject;
        goal.exerciseInfo!.name.Should().Be("On plan");
        goal.goalRecommendation.Should().NotBeNull();
    }

    // --- CreateGoal ---

    [Fact]
    public async Task CreateGoal_WithUnknownPlan_ReturnsNotFound()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var request = new CreateExerciseGoalRequest
        {
            exerciseInfoId = Guid.NewGuid(),
            workoutPlanId = Guid.NewGuid(),
            targetWeight = 140m,
            targetReps = 5,
            goalDate = new DateTime(2026, 6, 1)
        };

        var result = await ExerciseGoalHandler.CreateGoal(
            request,
            ClaimsPrincipalBuilder.WithUserId(Guid.NewGuid()),
            new ExerciseGoalRepository(dbContext),
            new WorkoutPlanRepository(dbContext),
            new ExerciseRepository(dbContext));

        result.Should().BeOfType<NotFound>();
        dbContext.ExerciseGoals.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateGoal_WithAnotherUsersPlan_ReturnsForbid()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var exerciseInfo = MakeExerciseInfo();
        var plan = MakePlan(Guid.NewGuid());
        dbContext.ExerciseInfos.Add(exerciseInfo);
        dbContext.WorkoutPlans.Add(plan);
        await dbContext.SaveChangesAsync();

        var request = new CreateExerciseGoalRequest
        {
            exerciseInfoId = exerciseInfo.Id,
            workoutPlanId = plan.Id,
            targetWeight = 140m,
            targetReps = 5,
            goalDate = new DateTime(2026, 6, 1)
        };

        var result = await ExerciseGoalHandler.CreateGoal(
            request,
            ClaimsPrincipalBuilder.WithUserId(Guid.NewGuid()),
            new ExerciseGoalRepository(dbContext),
            new WorkoutPlanRepository(dbContext),
            new ExerciseRepository(dbContext));

        result.Should().BeOfType<ForbidHttpResult>();
        dbContext.ExerciseGoals.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateGoal_WithExplicitStartValues_UsesThemAsGiven()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var userId = Guid.NewGuid();
        var exerciseInfo = MakeExerciseInfo();
        dbContext.ExerciseInfos.Add(exerciseInfo);
        await dbContext.SaveChangesAsync();

        var request = new CreateExerciseGoalRequest
        {
            exerciseInfoId = exerciseInfo.Id,
            startWeight = 95m,
            startReps = 8,
            targetWeight = 135m,
            targetReps = 5,
            goalDate = new DateTime(2026, 9, 1)
        };

        var result = await ExerciseGoalHandler.CreateGoal(
            request,
            ClaimsPrincipalBuilder.WithUserId(userId),
            new ExerciseGoalRepository(dbContext),
            new WorkoutPlanRepository(dbContext),
            new ExerciseRepository(dbContext));

        var ok = result.Should().BeOfType<Ok<ExerciseGoal>>().Subject;
        ok.Value!.startWeight.Should().Be(95m);
        ok.Value.startReps.Should().Be(8);
        ok.Value.targetWeight.Should().Be(135m);
        ok.Value.goalRecommendation.Should().BeNull();
    }

    [Fact]
    public async Task CreateGoal_WithoutStartValues_FallsBackToTheUsersBestSet()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var userId = Guid.NewGuid();
        var exerciseInfo = MakeExerciseInfo();
        dbContext.ExerciseInfos.Add(exerciseInfo);
        await dbContext.SaveChangesAsync();
        await SeedHistoryAsync(dbContext, userId, exerciseInfo.Id, 185m, 6);

        var request = new CreateExerciseGoalRequest
        {
            exerciseInfoId = exerciseInfo.Id,
            targetWeight = 225m,
            targetReps = 5,
            goalDate = new DateTime(2026, 9, 1)
        };

        var result = await ExerciseGoalHandler.CreateGoal(
            request,
            ClaimsPrincipalBuilder.WithUserId(userId),
            new ExerciseGoalRepository(dbContext),
            new WorkoutPlanRepository(dbContext),
            new ExerciseRepository(dbContext));

        var ok = result.Should().BeOfType<Ok<ExerciseGoal>>().Subject;
        ok.Value!.startWeight.Should().Be(185m);
        ok.Value.startReps.Should().Be(6);
    }

    [Fact]
    public async Task CreateGoal_WithoutStartValuesAndNoHistory_StartsFromZero()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var exerciseInfo = MakeExerciseInfo();
        dbContext.ExerciseInfos.Add(exerciseInfo);
        await dbContext.SaveChangesAsync();

        var request = new CreateExerciseGoalRequest
        {
            exerciseInfoId = exerciseInfo.Id,
            targetWeight = 225m,
            targetReps = 5,
            goalDate = new DateTime(2026, 9, 1)
        };

        var result = await ExerciseGoalHandler.CreateGoal(
            request,
            ClaimsPrincipalBuilder.WithUserId(Guid.NewGuid()),
            new ExerciseGoalRepository(dbContext),
            new WorkoutPlanRepository(dbContext),
            new ExerciseRepository(dbContext));

        var ok = result.Should().BeOfType<Ok<ExerciseGoal>>().Subject;
        ok.Value!.startWeight.Should().Be(0m);
        ok.Value.startReps.Should().Be(0);
    }

    [Fact]
    public async Task CreateGoal_OnAPlan_IncludesARecommendation()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var userId = Guid.NewGuid();
        var exerciseInfo = MakeExerciseInfo();
        var plan = MakePlan(userId);
        dbContext.ExerciseInfos.Add(exerciseInfo);
        dbContext.WorkoutPlans.Add(plan);
        await dbContext.SaveChangesAsync();

        var request = new CreateExerciseGoalRequest
        {
            exerciseInfoId = exerciseInfo.Id,
            workoutPlanId = plan.Id,
            startWeight = 100m,
            startReps = 5,
            targetWeight = 140m,
            targetReps = 5,
            goalDate = new DateTime(2026, 6, 1)
        };

        var result = await ExerciseGoalHandler.CreateGoal(
            request,
            ClaimsPrincipalBuilder.WithUserId(userId),
            new ExerciseGoalRepository(dbContext),
            new WorkoutPlanRepository(dbContext),
            new ExerciseRepository(dbContext));

        var ok = result.Should().BeOfType<Ok<ExerciseGoal>>().Subject;
        ok.Value!.workoutPlanId.Should().Be(plan.Id);
        ok.Value.goalRecommendation.Should().NotBeNull();
        ok.Value.goalRecommendation!.weight.Should().Be(140m);
    }

    // --- UpdateGoal ---

    [Fact]
    public async Task UpdateGoal_WithOwnGoal_ReturnsOkAndSavesTheNewNumbers()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var userId = Guid.NewGuid();
        var exerciseInfo = MakeExerciseInfo();
        var goal = MakeGoal(userId, exerciseInfo.Id);
        dbContext.ExerciseInfos.Add(exerciseInfo);
        dbContext.ExerciseGoals.Add(goal);
        await dbContext.SaveChangesAsync();

        var request = new UpdateExerciseGoalRequest
        {
            startWeight = 115m,
            startReps = 3,
            targetWeight = 155m,
            targetReps = 2,
            goalDate = new DateTime(2026, 11, 1)
        };

        var result = await ExerciseGoalHandler.UpdateGoal(
            goal.Id, request, ClaimsPrincipalBuilder.WithUserId(userId), new ExerciseGoalRepository(dbContext));

        result.Should().BeOfType<Ok>();
        var saved = dbContext.ExerciseGoals.Single(g => g.Id == goal.Id);
        saved.StartWeight.Should().Be(115m);
        saved.TargetWeight.Should().Be(155m);
        saved.GoalDate.Should().Be(new DateTime(2026, 11, 1));
    }

    [Fact]
    public async Task UpdateGoal_WithAnotherUsersGoal_ReturnsNotFound()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var exerciseInfo = MakeExerciseInfo();
        var goal = MakeGoal(Guid.NewGuid(), exerciseInfo.Id);
        dbContext.ExerciseInfos.Add(exerciseInfo);
        dbContext.ExerciseGoals.Add(goal);
        await dbContext.SaveChangesAsync();

        var request = new UpdateExerciseGoalRequest { startWeight = 1m, startReps = 1, targetWeight = 1m, targetReps = 1 };

        var result = await ExerciseGoalHandler.UpdateGoal(
            goal.Id, request, ClaimsPrincipalBuilder.WithUserId(Guid.NewGuid()), new ExerciseGoalRepository(dbContext));

        result.Should().BeOfType<NotFound>();
        dbContext.ExerciseGoals.Single(g => g.Id == goal.Id).TargetWeight.Should().Be(140m);
    }

    [Fact]
    public async Task UpdateGoal_WithUnknownGoal_ReturnsNotFound()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var request = new UpdateExerciseGoalRequest { startWeight = 1m, startReps = 1, targetWeight = 1m, targetReps = 1 };

        var result = await ExerciseGoalHandler.UpdateGoal(
            Guid.NewGuid(), request, ClaimsPrincipalBuilder.WithUserId(Guid.NewGuid()), new ExerciseGoalRepository(dbContext));

        result.Should().BeOfType<NotFound>();
    }

    // --- AssignGoalToPlan ---

    [Fact]
    public async Task AssignGoalToPlan_WithUnknownPlan_ReturnsNotFound()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var request = new AssignGoalToPlanRequest { workoutPlanId = Guid.NewGuid() };

        var result = await ExerciseGoalHandler.AssignGoalToPlan(
            Guid.NewGuid(),
            request,
            ClaimsPrincipalBuilder.WithUserId(Guid.NewGuid()),
            new ExerciseGoalRepository(dbContext),
            new WorkoutPlanRepository(dbContext));

        result.Should().BeOfType<NotFound>();
    }

    [Fact]
    public async Task AssignGoalToPlan_WithAnotherUsersPlan_ReturnsForbid()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var plan = MakePlan(Guid.NewGuid());
        dbContext.WorkoutPlans.Add(plan);
        await dbContext.SaveChangesAsync();
        var request = new AssignGoalToPlanRequest { workoutPlanId = plan.Id };

        var result = await ExerciseGoalHandler.AssignGoalToPlan(
            Guid.NewGuid(),
            request,
            ClaimsPrincipalBuilder.WithUserId(Guid.NewGuid()),
            new ExerciseGoalRepository(dbContext),
            new WorkoutPlanRepository(dbContext));

        result.Should().BeOfType<ForbidHttpResult>();
    }

    [Fact]
    public async Task AssignGoalToPlan_WithOwnGoalAndPlan_ReturnsOkAndAttachesIt()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var userId = Guid.NewGuid();
        var exerciseInfo = MakeExerciseInfo();
        var goal = MakeGoal(userId, exerciseInfo.Id);
        var plan = MakePlan(userId);
        dbContext.ExerciseInfos.Add(exerciseInfo);
        dbContext.ExerciseGoals.Add(goal);
        dbContext.WorkoutPlans.Add(plan);
        await dbContext.SaveChangesAsync();

        var result = await ExerciseGoalHandler.AssignGoalToPlan(
            goal.Id,
            new AssignGoalToPlanRequest { workoutPlanId = plan.Id },
            ClaimsPrincipalBuilder.WithUserId(userId),
            new ExerciseGoalRepository(dbContext),
            new WorkoutPlanRepository(dbContext));

        result.Should().BeOfType<Ok>();
        dbContext.ExerciseGoals.Single(g => g.Id == goal.Id).WorkoutPlanId.Should().Be(plan.Id);
    }

    [Fact]
    public async Task AssignGoalToPlan_WithNoPlanId_DetachesTheGoal()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var userId = Guid.NewGuid();
        var exerciseInfo = MakeExerciseInfo();
        var plan = MakePlan(userId);
        var goal = MakeGoal(userId, exerciseInfo.Id, plan.Id);
        dbContext.ExerciseInfos.Add(exerciseInfo);
        dbContext.WorkoutPlans.Add(plan);
        dbContext.ExerciseGoals.Add(goal);
        await dbContext.SaveChangesAsync();

        var result = await ExerciseGoalHandler.AssignGoalToPlan(
            goal.Id,
            new AssignGoalToPlanRequest { workoutPlanId = null },
            ClaimsPrincipalBuilder.WithUserId(userId),
            new ExerciseGoalRepository(dbContext),
            new WorkoutPlanRepository(dbContext));

        result.Should().BeOfType<Ok>();
        dbContext.ExerciseGoals.Single(g => g.Id == goal.Id).WorkoutPlanId.Should().BeNull();
    }

    [Fact]
    public async Task AssignGoalToPlan_WithUnknownGoal_ReturnsNotFound()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var userId = Guid.NewGuid();
        var plan = MakePlan(userId);
        dbContext.WorkoutPlans.Add(plan);
        await dbContext.SaveChangesAsync();

        var result = await ExerciseGoalHandler.AssignGoalToPlan(
            Guid.NewGuid(),
            new AssignGoalToPlanRequest { workoutPlanId = plan.Id },
            ClaimsPrincipalBuilder.WithUserId(userId),
            new ExerciseGoalRepository(dbContext),
            new WorkoutPlanRepository(dbContext));

        result.Should().BeOfType<NotFound>();
    }

    // --- DeleteGoal ---

    [Fact]
    public async Task DeleteGoal_WithOwnGoal_ReturnsOkAndRemovesIt()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var userId = Guid.NewGuid();
        var exerciseInfo = MakeExerciseInfo();
        var goal = MakeGoal(userId, exerciseInfo.Id);
        dbContext.ExerciseInfos.Add(exerciseInfo);
        dbContext.ExerciseGoals.Add(goal);
        await dbContext.SaveChangesAsync();

        var result = await ExerciseGoalHandler.DeleteGoal(
            goal.Id, ClaimsPrincipalBuilder.WithUserId(userId), new ExerciseGoalRepository(dbContext));

        result.Should().BeOfType<Ok>();
        dbContext.ExerciseGoals.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteGoal_WithAnotherUsersGoal_ReturnsNotFoundAndKeepsIt()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var exerciseInfo = MakeExerciseInfo();
        var goal = MakeGoal(Guid.NewGuid(), exerciseInfo.Id);
        dbContext.ExerciseInfos.Add(exerciseInfo);
        dbContext.ExerciseGoals.Add(goal);
        await dbContext.SaveChangesAsync();

        var result = await ExerciseGoalHandler.DeleteGoal(
            goal.Id, ClaimsPrincipalBuilder.WithUserId(Guid.NewGuid()), new ExerciseGoalRepository(dbContext));

        result.Should().BeOfType<NotFound>();
        dbContext.ExerciseGoals.Should().ContainSingle();
    }

    [Fact]
    public async Task DeleteGoal_WithUnknownGoal_ReturnsNotFound()
    {
        var dbContext = DbContextFactory.CreateInMemory();

        var result = await ExerciseGoalHandler.DeleteGoal(
            Guid.NewGuid(), ClaimsPrincipalBuilder.WithUserId(Guid.NewGuid()), new ExerciseGoalRepository(dbContext));

        result.Should().BeOfType<NotFound>();
    }
}
