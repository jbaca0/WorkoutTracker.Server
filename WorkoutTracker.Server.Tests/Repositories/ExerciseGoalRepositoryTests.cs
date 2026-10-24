using FluentAssertions;
using WorkoutTracker.Server.Models.Repositories;
using WorkoutTracker.Server.Tests.Helpers;
using Xunit;

namespace WorkoutTracker.Server.Tests.Repositories;

public class ExerciseGoalRepositoryTests
{
    private static Models.ExerciseInfo MakeExerciseInfo(string name = "Bench Press") =>
        new() { Id = Guid.NewGuid(), Name = name, SecondaryMuscleGroupsJson = "[]" };

    private static Models.ExerciseGoal MakeGoal(
        Guid userId,
        Guid exerciseInfoId,
        Guid? planId = null,
        DateTime? createdAt = null) =>
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
            CreatedAt = createdAt ?? new DateTime(2026, 1, 1)
        };

    // --- GetByUserIdAsync ---

    [Fact]
    public async Task GetByUserIdAsync_ReturnsOnlyTheUsersGoalsWithExerciseInfoLoaded()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var userId = Guid.NewGuid();
        var mine = MakeExerciseInfo("Squat");
        var theirs = MakeExerciseInfo("Row");
        dbContext.ExerciseInfos.AddRange(mine, theirs);
        dbContext.ExerciseGoals.AddRange(
            MakeGoal(userId, mine.Id),
            MakeGoal(Guid.NewGuid(), theirs.Id));
        await dbContext.SaveChangesAsync();

        var result = await new ExerciseGoalRepository(dbContext).GetByUserIdAsync(userId);

        result.Should().ContainSingle();
        result[0].ExerciseInfo!.Name.Should().Be("Squat");
    }

    [Fact]
    public async Task GetByUserIdAsync_OrdersByCreatedAt()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var userId = Guid.NewGuid();
        var first = MakeExerciseInfo("First");
        var second = MakeExerciseInfo("Second");
        dbContext.ExerciseInfos.AddRange(first, second);
        dbContext.ExerciseGoals.AddRange(
            MakeGoal(userId, second.Id, createdAt: new DateTime(2026, 2, 1)),
            MakeGoal(userId, first.Id, createdAt: new DateTime(2026, 1, 1)));
        await dbContext.SaveChangesAsync();

        var result = await new ExerciseGoalRepository(dbContext).GetByUserIdAsync(userId);

        result.Select(g => g.ExerciseInfo!.Name).Should().Equal("First", "Second");
    }

    [Fact]
    public async Task GetByUserIdAsync_WithNoGoals_ReturnsEmptyList()
    {
        var dbContext = DbContextFactory.CreateInMemory();

        var result = await new ExerciseGoalRepository(dbContext).GetByUserIdAsync(Guid.NewGuid());

        result.Should().BeEmpty();
    }

    // --- GetByExerciseInfoIdAsync ---

    [Fact]
    public async Task GetByExerciseInfoIdAsync_ReturnsTheUsersGoalForThatExercise()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var userId = Guid.NewGuid();
        var exerciseInfo = MakeExerciseInfo();
        dbContext.ExerciseInfos.Add(exerciseInfo);
        dbContext.ExerciseGoals.Add(MakeGoal(userId, exerciseInfo.Id));
        await dbContext.SaveChangesAsync();

        var result = await new ExerciseGoalRepository(dbContext).GetByExerciseInfoIdAsync(userId, exerciseInfo.Id);

        result.Should().NotBeNull();
        result!.TargetWeight.Should().Be(140m);
    }

    [Fact]
    public async Task GetByExerciseInfoIdAsync_DoesNotReturnAnotherUsersGoal()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var exerciseInfo = MakeExerciseInfo();
        dbContext.ExerciseInfos.Add(exerciseInfo);
        dbContext.ExerciseGoals.Add(MakeGoal(Guid.NewGuid(), exerciseInfo.Id));
        await dbContext.SaveChangesAsync();

        var result = await new ExerciseGoalRepository(dbContext).GetByExerciseInfoIdAsync(Guid.NewGuid(), exerciseInfo.Id);

        result.Should().BeNull();
    }

    // --- GetByExerciseInfoIdsAsync ---

    [Fact]
    public async Task GetByExerciseInfoIdsAsync_ReturnsOnlyTheRequestedExercises()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var userId = Guid.NewGuid();
        var wanted = MakeExerciseInfo("Wanted");
        var other = MakeExerciseInfo("Other");
        dbContext.ExerciseInfos.AddRange(wanted, other);
        dbContext.ExerciseGoals.AddRange(MakeGoal(userId, wanted.Id), MakeGoal(userId, other.Id));
        await dbContext.SaveChangesAsync();

        var result = await new ExerciseGoalRepository(dbContext)
            .GetByExerciseInfoIdsAsync(userId, new List<Guid> { wanted.Id });

        result.Should().ContainSingle().Which.ExerciseInfoId.Should().Be(wanted.Id);
    }

    [Fact]
    public async Task GetByExerciseInfoIdsAsync_WithEmptyIdList_ReturnsEmptyList()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var userId = Guid.NewGuid();
        var exerciseInfo = MakeExerciseInfo();
        dbContext.ExerciseInfos.Add(exerciseInfo);
        dbContext.ExerciseGoals.Add(MakeGoal(userId, exerciseInfo.Id));
        await dbContext.SaveChangesAsync();

        var result = await new ExerciseGoalRepository(dbContext)
            .GetByExerciseInfoIdsAsync(userId, new List<Guid>());

        result.Should().BeEmpty();
    }

    // --- GetByPlanIdAsync ---

    [Fact]
    public async Task GetByPlanIdAsync_ReturnsOnlyGoalsAssignedToThatPlan()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var userId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        var assigned = MakeExerciseInfo("Assigned");
        var unassigned = MakeExerciseInfo("Unassigned");
        dbContext.ExerciseInfos.AddRange(assigned, unassigned);
        dbContext.ExerciseGoals.AddRange(
            MakeGoal(userId, assigned.Id, planId),
            MakeGoal(userId, unassigned.Id));
        await dbContext.SaveChangesAsync();

        var result = await new ExerciseGoalRepository(dbContext).GetByPlanIdAsync(userId, planId);

        result.Should().ContainSingle().Which.ExerciseInfo!.Name.Should().Be("Assigned");
    }

    // --- GetModelByIdAsync ---

    [Fact]
    public async Task GetModelByIdAsync_ReturnsTheGoalModel()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var exerciseInfo = MakeExerciseInfo();
        var goal = MakeGoal(Guid.NewGuid(), exerciseInfo.Id);
        dbContext.ExerciseInfos.Add(exerciseInfo);
        dbContext.ExerciseGoals.Add(goal);
        await dbContext.SaveChangesAsync();

        var result = await new ExerciseGoalRepository(dbContext).GetModelByIdAsync(goal.Id);

        result.Should().NotBeNull();
        result!.Id.Should().Be(goal.Id);
    }

    [Fact]
    public async Task GetModelByIdAsync_WithUnknownId_ReturnsNull()
    {
        var dbContext = DbContextFactory.CreateInMemory();

        var result = await new ExerciseGoalRepository(dbContext).GetModelByIdAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    // --- CreateAsync ---

    [Fact]
    public async Task CreateAsync_PersistsTheGoalAndReturnsItWithExerciseInfo()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var userId = Guid.NewGuid();
        var exerciseInfo = MakeExerciseInfo("Overhead Press");
        dbContext.ExerciseInfos.Add(exerciseInfo);
        await dbContext.SaveChangesAsync();

        var goal = await new ExerciseGoalRepository(dbContext).CreateAsync(
            userId, exerciseInfo.Id, null, 60m, 8, 90m, 5, new DateTime(2026, 9, 1));

        goal.UserId.Should().Be(userId);
        goal.StartWeight.Should().Be(60m);
        goal.StartReps.Should().Be(8);
        goal.TargetWeight.Should().Be(90m);
        goal.TargetReps.Should().Be(5);
        goal.GoalDate.Should().Be(new DateTime(2026, 9, 1));
        goal.ExerciseInfo!.Name.Should().Be("Overhead Press");
        dbContext.ExerciseGoals.Should().ContainSingle();
    }

    [Fact]
    public async Task CreateAsync_ReplacesAnExistingGoalForTheSameExercise()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var userId = Guid.NewGuid();
        var exerciseInfo = MakeExerciseInfo();
        dbContext.ExerciseInfos.Add(exerciseInfo);
        await dbContext.SaveChangesAsync();

        var repo = new ExerciseGoalRepository(dbContext);
        var original = await repo.CreateAsync(userId, exerciseInfo.Id, null, 100m, 5, 140m, 5, new DateTime(2026, 6, 1));
        var replacement = await repo.CreateAsync(userId, exerciseInfo.Id, null, 110m, 5, 150m, 5, new DateTime(2026, 7, 1));

        replacement.Id.Should().NotBe(original.Id);
        var remaining = dbContext.ExerciseGoals.Should().ContainSingle().Subject;
        remaining.Id.Should().Be(replacement.Id);
        remaining.TargetWeight.Should().Be(150m);
    }

    [Fact]
    public async Task CreateAsync_LeavesAnotherUsersGoalForTheSameExerciseInPlace()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var exerciseInfo = MakeExerciseInfo();
        var otherUsersGoal = MakeGoal(Guid.NewGuid(), exerciseInfo.Id);
        dbContext.ExerciseInfos.Add(exerciseInfo);
        dbContext.ExerciseGoals.Add(otherUsersGoal);
        await dbContext.SaveChangesAsync();

        await new ExerciseGoalRepository(dbContext).CreateAsync(
            Guid.NewGuid(), exerciseInfo.Id, null, 100m, 5, 140m, 5, new DateTime(2026, 6, 1));

        dbContext.ExerciseGoals.Should().HaveCount(2);
        dbContext.ExerciseGoals.Any(g => g.Id == otherUsersGoal.Id).Should().BeTrue();
    }

    [Fact]
    public async Task CreateAsync_StoresTheWorkoutPlanIdWhenOneIsGiven()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var userId = Guid.NewGuid();
        var exerciseInfo = MakeExerciseInfo();
        var plan = new Models.WorkoutPlan { Id = Guid.NewGuid(), UserId = userId, Name = "Plan", StartDate = DateTime.UtcNow };
        dbContext.ExerciseInfos.Add(exerciseInfo);
        dbContext.WorkoutPlans.Add(plan);
        await dbContext.SaveChangesAsync();

        var goal = await new ExerciseGoalRepository(dbContext).CreateAsync(
            userId, exerciseInfo.Id, plan.Id, 100m, 5, 140m, 5, new DateTime(2026, 6, 1));

        goal.WorkoutPlanId.Should().Be(plan.Id);
    }

    // --- UpdateAsync ---

    [Fact]
    public async Task UpdateAsync_OverwritesTheGoalsNumbersAndReturnsTrue()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var userId = Guid.NewGuid();
        var exerciseInfo = MakeExerciseInfo();
        var goal = MakeGoal(userId, exerciseInfo.Id);
        dbContext.ExerciseInfos.Add(exerciseInfo);
        dbContext.ExerciseGoals.Add(goal);
        await dbContext.SaveChangesAsync();

        var success = await new ExerciseGoalRepository(dbContext).UpdateAsync(
            goal.Id, userId, 120m, 3, 160m, 2, new DateTime(2026, 12, 1));

        success.Should().BeTrue();
        var saved = dbContext.ExerciseGoals.Single(g => g.Id == goal.Id);
        saved.StartWeight.Should().Be(120m);
        saved.StartReps.Should().Be(3);
        saved.TargetWeight.Should().Be(160m);
        saved.TargetReps.Should().Be(2);
        saved.GoalDate.Should().Be(new DateTime(2026, 12, 1));
    }

    [Fact]
    public async Task UpdateAsync_WithAnotherUsersGoal_ReturnsFalseAndChangesNothing()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var exerciseInfo = MakeExerciseInfo();
        var goal = MakeGoal(Guid.NewGuid(), exerciseInfo.Id);
        dbContext.ExerciseInfos.Add(exerciseInfo);
        dbContext.ExerciseGoals.Add(goal);
        await dbContext.SaveChangesAsync();

        var success = await new ExerciseGoalRepository(dbContext).UpdateAsync(
            goal.Id, Guid.NewGuid(), 999m, 1, 999m, 1, new DateTime(2027, 1, 1));

        success.Should().BeFalse();
        dbContext.ExerciseGoals.Single(g => g.Id == goal.Id).TargetWeight.Should().Be(140m);
    }

    [Fact]
    public async Task UpdateAsync_WithUnknownGoal_ReturnsFalse()
    {
        var dbContext = DbContextFactory.CreateInMemory();

        var success = await new ExerciseGoalRepository(dbContext).UpdateAsync(
            Guid.NewGuid(), Guid.NewGuid(), 100m, 5, 140m, 5, DateTime.UtcNow);

        success.Should().BeFalse();
    }

    // --- AssignToPlanAsync ---

    [Fact]
    public async Task AssignToPlanAsync_AttachesTheGoalToThePlan()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var userId = Guid.NewGuid();
        var exerciseInfo = MakeExerciseInfo();
        var goal = MakeGoal(userId, exerciseInfo.Id);
        var plan = new Models.WorkoutPlan { Id = Guid.NewGuid(), UserId = userId, Name = "Plan", StartDate = DateTime.UtcNow };
        dbContext.ExerciseInfos.Add(exerciseInfo);
        dbContext.ExerciseGoals.Add(goal);
        dbContext.WorkoutPlans.Add(plan);
        await dbContext.SaveChangesAsync();

        var success = await new ExerciseGoalRepository(dbContext).AssignToPlanAsync(goal.Id, userId, plan.Id);

        success.Should().BeTrue();
        dbContext.ExerciseGoals.Single(g => g.Id == goal.Id).WorkoutPlanId.Should().Be(plan.Id);
    }

    [Fact]
    public async Task AssignToPlanAsync_WithNullPlanId_DetachesTheGoal()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var userId = Guid.NewGuid();
        var exerciseInfo = MakeExerciseInfo();
        var goal = MakeGoal(userId, exerciseInfo.Id, Guid.NewGuid());
        dbContext.ExerciseInfos.Add(exerciseInfo);
        dbContext.ExerciseGoals.Add(goal);
        await dbContext.SaveChangesAsync();

        var success = await new ExerciseGoalRepository(dbContext).AssignToPlanAsync(goal.Id, userId, null);

        success.Should().BeTrue();
        dbContext.ExerciseGoals.Single(g => g.Id == goal.Id).WorkoutPlanId.Should().BeNull();
    }

    [Fact]
    public async Task AssignToPlanAsync_WithAnotherUsersGoal_ReturnsFalse()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var exerciseInfo = MakeExerciseInfo();
        var goal = MakeGoal(Guid.NewGuid(), exerciseInfo.Id);
        dbContext.ExerciseInfos.Add(exerciseInfo);
        dbContext.ExerciseGoals.Add(goal);
        await dbContext.SaveChangesAsync();

        var success = await new ExerciseGoalRepository(dbContext)
            .AssignToPlanAsync(goal.Id, Guid.NewGuid(), Guid.NewGuid());

        success.Should().BeFalse();
        dbContext.ExerciseGoals.Single(g => g.Id == goal.Id).WorkoutPlanId.Should().BeNull();
    }

    // --- DeleteAsync ---

    [Fact]
    public async Task DeleteAsync_RemovesTheUsersGoalAndReturnsTrue()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var userId = Guid.NewGuid();
        var exerciseInfo = MakeExerciseInfo();
        var goal = MakeGoal(userId, exerciseInfo.Id);
        dbContext.ExerciseInfos.Add(exerciseInfo);
        dbContext.ExerciseGoals.Add(goal);
        await dbContext.SaveChangesAsync();

        var success = await new ExerciseGoalRepository(dbContext).DeleteAsync(goal.Id, userId);

        success.Should().BeTrue();
        dbContext.ExerciseGoals.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteAsync_WithAnotherUsersGoal_ReturnsFalseAndKeepsIt()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var exerciseInfo = MakeExerciseInfo();
        var goal = MakeGoal(Guid.NewGuid(), exerciseInfo.Id);
        dbContext.ExerciseInfos.Add(exerciseInfo);
        dbContext.ExerciseGoals.Add(goal);
        await dbContext.SaveChangesAsync();

        var success = await new ExerciseGoalRepository(dbContext).DeleteAsync(goal.Id, Guid.NewGuid());

        success.Should().BeFalse();
        dbContext.ExerciseGoals.Should().ContainSingle();
    }

    [Fact]
    public async Task DeleteAsync_WithUnknownGoal_ReturnsFalse()
    {
        var dbContext = DbContextFactory.CreateInMemory();

        var success = await new ExerciseGoalRepository(dbContext).DeleteAsync(Guid.NewGuid(), Guid.NewGuid());

        success.Should().BeFalse();
    }
}
