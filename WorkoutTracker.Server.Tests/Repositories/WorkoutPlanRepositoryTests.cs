using FluentAssertions;
using WorkoutTracker.Server.DataTransferObjects.Requests.WorkoutPlan;
using WorkoutTracker.Server.Models.Repositories;
using WorkoutTracker.Server.Tests.Helpers;
using Xunit;

namespace WorkoutTracker.Server.Tests.Repositories;

public class WorkoutPlanRepositoryTests
{
    private static WorkoutPlanDayRequest DayRequest(int dayOfWeek, Guid? templateId = null, string? note = null) =>
        new() { dayOfWeek = dayOfWeek, workoutTemplateId = templateId, note = note };

    private static Models.WorkoutPlan MakePlan(Guid userId, string name = "Plan", bool isActive = false, DateTime? created = null) =>
        new()
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = name,
            IsActive = isActive,
            StartDate = new DateTime(2026, 1, 1),
            CreatedDateTime = created ?? new DateTime(2026, 1, 1)
        };

    // --- GetAllForUserAsync ---

    [Fact]
    public async Task GetAllForUserAsync_ReturnsOnlyPlansOwnedByTheUser()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var userId = Guid.NewGuid();
        dbContext.WorkoutPlans.AddRange(MakePlan(userId, "Mine"), MakePlan(Guid.NewGuid(), "Theirs"));
        await dbContext.SaveChangesAsync();

        var result = await new WorkoutPlanRepository(dbContext).GetAllForUserAsync(userId);

        result.Should().ContainSingle().Which.Name.Should().Be("Mine");
    }

    [Fact]
    public async Task GetAllForUserAsync_OrdersByCreatedDateTime()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var userId = Guid.NewGuid();
        dbContext.WorkoutPlans.AddRange(
            MakePlan(userId, "Second", created: new DateTime(2026, 3, 1)),
            MakePlan(userId, "First", created: new DateTime(2026, 2, 1)));
        await dbContext.SaveChangesAsync();

        var result = await new WorkoutPlanRepository(dbContext).GetAllForUserAsync(userId);

        result.Select(p => p.Name).Should().Equal("First", "Second");
    }

    [Fact]
    public async Task GetAllForUserAsync_IncludesDaysAndTheirTemplates()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var userId = Guid.NewGuid();
        var template = new Models.WorkoutTemplate { Id = Guid.NewGuid(), Name = "Leg day", UserId = userId };
        var plan = MakePlan(userId);
        dbContext.WorkoutTemplates.Add(template);
        dbContext.WorkoutPlans.Add(plan);
        dbContext.WorkoutPlanDays.Add(new Models.WorkoutPlanDay
        {
            Id = Guid.NewGuid(),
            WorkoutPlanId = plan.Id,
            DayOfWeek = 3,
            WorkoutTemplateId = template.Id
        });
        await dbContext.SaveChangesAsync();

        var result = await new WorkoutPlanRepository(dbContext).GetAllForUserAsync(userId);

        var day = result.Should().ContainSingle().Subject.Days.Should().ContainSingle().Subject;
        day.DayOfWeek.Should().Be(3);
        day.WorkoutTemplate!.Name.Should().Be("Leg day");
    }

    // --- GetByIdAsync / GetByIdWithDetailsAsync ---

    [Fact]
    public async Task GetByIdAsync_ReturnsThePlanModel()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var plan = MakePlan(Guid.NewGuid(), "Findable");
        dbContext.WorkoutPlans.Add(plan);
        await dbContext.SaveChangesAsync();

        var result = await new WorkoutPlanRepository(dbContext).GetByIdAsync(plan.Id);

        result.Should().NotBeNull();
        result!.Name.Should().Be("Findable");
    }

    [Fact]
    public async Task GetByIdAsync_WithUnknownId_ReturnsNull()
    {
        var dbContext = DbContextFactory.CreateInMemory();

        var result = await new WorkoutPlanRepository(dbContext).GetByIdAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdWithDetailsAsync_LoadsDaysTemplatesAndTheirExercises()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var userId = Guid.NewGuid();
        var exerciseInfo = new Models.ExerciseInfo { Id = Guid.NewGuid(), Name = "Deadlift", SecondaryMuscleGroupsJson = "[]" };
        var template = new Models.WorkoutTemplate { Id = Guid.NewGuid(), Name = "Pull", UserId = userId };
        template.Exercises.Add(new Models.WorkoutTemplateExercise
        {
            Id = Guid.NewGuid(),
            WorkoutTemplateId = template.Id,
            ExerciseInfoId = exerciseInfo.Id,
            Order = 0
        });
        var plan = MakePlan(userId);
        dbContext.ExerciseInfos.Add(exerciseInfo);
        dbContext.WorkoutTemplates.Add(template);
        dbContext.WorkoutPlans.Add(plan);
        dbContext.WorkoutPlanDays.Add(new Models.WorkoutPlanDay
        {
            Id = Guid.NewGuid(),
            WorkoutPlanId = plan.Id,
            DayOfWeek = 1,
            WorkoutTemplateId = template.Id
        });
        await dbContext.SaveChangesAsync();

        var result = await new WorkoutPlanRepository(dbContext).GetByIdWithDetailsAsync(plan.Id);

        var day = result!.Days.Should().ContainSingle().Subject;
        var exercise = day.WorkoutTemplate!.Exercises.Should().ContainSingle().Subject;
        exercise.ExerciseInfo!.Name.Should().Be("Deadlift");
    }

    [Fact]
    public async Task GetByIdWithDetailsAsync_WithUnknownId_ReturnsNull()
    {
        var dbContext = DbContextFactory.CreateInMemory();

        var result = await new WorkoutPlanRepository(dbContext).GetByIdWithDetailsAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    // --- AddAsync ---

    [Fact]
    public async Task AddAsync_ActivatesTheFirstPlanAUserCreates()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var userId = Guid.NewGuid();

        var planId = await new WorkoutPlanRepository(dbContext).AddAsync(
            userId, "First plan", new DateTime(2026, 1, 1), null, new List<WorkoutPlanDayRequest>());

        dbContext.WorkoutPlans.Single(p => p.Id == planId).IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task AddAsync_LeavesTheNewPlanInactiveWhenTheUserAlreadyHasAnActiveOne()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var userId = Guid.NewGuid();
        dbContext.WorkoutPlans.Add(MakePlan(userId, "Existing", isActive: true));
        await dbContext.SaveChangesAsync();

        var planId = await new WorkoutPlanRepository(dbContext).AddAsync(
            userId, "Second plan", new DateTime(2026, 1, 1), null, new List<WorkoutPlanDayRequest>());

        dbContext.WorkoutPlans.Single(p => p.Id == planId).IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task AddAsync_IgnoresAnotherUsersActivePlanWhenDecidingToActivate()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        dbContext.WorkoutPlans.Add(MakePlan(Guid.NewGuid(), "Someone else's active plan", isActive: true));
        await dbContext.SaveChangesAsync();

        var planId = await new WorkoutPlanRepository(dbContext).AddAsync(
            Guid.NewGuid(), "Mine", new DateTime(2026, 1, 1), null, new List<WorkoutPlanDayRequest>());

        dbContext.WorkoutPlans.Single(p => p.Id == planId).IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task AddAsync_PersistsNameDatesAndDays()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var templateId = Guid.NewGuid();
        dbContext.WorkoutTemplates.Add(new Models.WorkoutTemplate { Id = templateId, Name = "Push", UserId = Guid.NewGuid() });
        await dbContext.SaveChangesAsync();

        var repo = new WorkoutPlanRepository(dbContext);
        var planId = await repo.AddAsync(
            Guid.NewGuid(),
            "Spring block",
            new DateTime(2026, 4, 1),
            new DateTime(2026, 6, 1),
            new List<WorkoutPlanDayRequest> { DayRequest(1, templateId, "Heavy"), DayRequest(4, note: "Rest") });

        var plan = dbContext.WorkoutPlans.Single(p => p.Id == planId);
        plan.Name.Should().Be("Spring block");
        plan.StartDate.Should().Be(new DateTime(2026, 4, 1));
        plan.EndDate.Should().Be(new DateTime(2026, 6, 1));
        var days = dbContext.WorkoutPlanDays.Where(d => d.WorkoutPlanId == planId).OrderBy(d => d.DayOfWeek).ToList();
        days.Select(d => d.DayOfWeek).Should().Equal(1, 4);
        days[0].WorkoutTemplateId.Should().Be(templateId);
        days[0].Note.Should().Be("Heavy");
        days[1].WorkoutTemplateId.Should().BeNull();
    }

    // --- UpdateAsync ---

    [Fact]
    public async Task UpdateAsync_UpdatesNameDatesAndReplacesDays()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var repo = new WorkoutPlanRepository(dbContext);
        var planId = await repo.AddAsync(
            Guid.NewGuid(), "Before", new DateTime(2026, 1, 1), null,
            new List<WorkoutPlanDayRequest> { DayRequest(1), DayRequest(2) });

        var success = await repo.UpdateAsync(
            planId, "After", new DateTime(2026, 2, 1), new DateTime(2026, 3, 1),
            new List<WorkoutPlanDayRequest> { DayRequest(5, note: "Only day") });

        success.Should().BeTrue();
        var plan = dbContext.WorkoutPlans.Single(p => p.Id == planId);
        plan.Name.Should().Be("After");
        plan.StartDate.Should().Be(new DateTime(2026, 2, 1));
        plan.EndDate.Should().Be(new DateTime(2026, 3, 1));
        var day = dbContext.WorkoutPlanDays.Where(d => d.WorkoutPlanId == planId).Should().ContainSingle().Subject;
        day.DayOfWeek.Should().Be(5);
        day.Note.Should().Be("Only day");
    }

    [Fact]
    public async Task UpdateAsync_WithManyExistingDays_DoesNotThrowWhileClearingThem()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var repo = new WorkoutPlanRepository(dbContext);
        var planId = await repo.AddAsync(
            Guid.NewGuid(), "Before", new DateTime(2026, 1, 1), null,
            Enumerable.Range(0, 7).Select(d => DayRequest(d)).ToList());

        var update = async () => await repo.UpdateAsync(
            planId, "After", new DateTime(2026, 1, 1), null,
            new List<WorkoutPlanDayRequest> { DayRequest(0) });

        await update.Should().NotThrowAsync();
        dbContext.WorkoutPlanDays.Where(d => d.WorkoutPlanId == planId).Should().ContainSingle();
    }

    [Fact]
    public async Task UpdateAsync_WithUnknownPlan_ReturnsFalse()
    {
        var dbContext = DbContextFactory.CreateInMemory();

        var success = await new WorkoutPlanRepository(dbContext).UpdateAsync(
            Guid.NewGuid(), "Nope", DateTime.UtcNow, null, new List<WorkoutPlanDayRequest>());

        success.Should().BeFalse();
    }

    // --- DeleteAsync ---

    [Fact]
    public async Task DeleteAsync_RemovesThePlanAndReturnsTrue()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var plan = MakePlan(Guid.NewGuid());
        dbContext.WorkoutPlans.Add(plan);
        await dbContext.SaveChangesAsync();

        var success = await new WorkoutPlanRepository(dbContext).DeleteAsync(plan.Id);

        success.Should().BeTrue();
        dbContext.WorkoutPlans.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteAsync_WithUnknownPlan_ReturnsFalse()
    {
        var dbContext = DbContextFactory.CreateInMemory();

        var success = await new WorkoutPlanRepository(dbContext).DeleteAsync(Guid.NewGuid());

        success.Should().BeFalse();
    }

    // --- ActivateAsync ---

    [Fact]
    public async Task ActivateAsync_ActivatesTheChosenPlanAndDeactivatesTheUsersOthers()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var userId = Guid.NewGuid();
        var current = MakePlan(userId, "Current", isActive: true);
        var wanted = MakePlan(userId, "Wanted");
        dbContext.WorkoutPlans.AddRange(current, wanted);
        await dbContext.SaveChangesAsync();

        await new WorkoutPlanRepository(dbContext).ActivateAsync(userId, wanted.Id);

        dbContext.WorkoutPlans.Single(p => p.Id == wanted.Id).IsActive.Should().BeTrue();
        dbContext.WorkoutPlans.Single(p => p.Id == current.Id).IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task ActivateAsync_LeavesOtherUsersPlansUntouched()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var userId = Guid.NewGuid();
        var mine = MakePlan(userId, "Mine");
        var theirs = MakePlan(Guid.NewGuid(), "Theirs", isActive: true);
        dbContext.WorkoutPlans.AddRange(mine, theirs);
        await dbContext.SaveChangesAsync();

        await new WorkoutPlanRepository(dbContext).ActivateAsync(userId, mine.Id);

        dbContext.WorkoutPlans.Single(p => p.Id == theirs.Id).IsActive.Should().BeTrue();
    }

    // --- GetTodaysPlanDayAsync ---

    [Fact]
    public async Task GetTodaysPlanDayAsync_ReturnsTheMatchingDayOfTheActivePlan()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var userId = Guid.NewGuid();
        var template = new Models.WorkoutTemplate { Id = Guid.NewGuid(), Name = "Wednesday push", UserId = userId };
        var plan = MakePlan(userId, isActive: true);
        dbContext.WorkoutTemplates.Add(template);
        dbContext.WorkoutPlans.Add(plan);
        dbContext.WorkoutPlanDays.AddRange(
            new Models.WorkoutPlanDay { Id = Guid.NewGuid(), WorkoutPlanId = plan.Id, DayOfWeek = 3, WorkoutTemplateId = template.Id, Note = "Push" },
            new Models.WorkoutPlanDay { Id = Guid.NewGuid(), WorkoutPlanId = plan.Id, DayOfWeek = 5, Note = "Legs" });
        await dbContext.SaveChangesAsync();

        var result = await new WorkoutPlanRepository(dbContext).GetTodaysPlanDayAsync(userId, 3);

        result.Should().NotBeNull();
        result!.Note.Should().Be("Push");
        result.WorkoutTemplate!.Name.Should().Be("Wednesday push");
    }

    [Fact]
    public async Task GetTodaysPlanDayAsync_IgnoresInactivePlans()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var userId = Guid.NewGuid();
        var plan = MakePlan(userId, isActive: false);
        dbContext.WorkoutPlans.Add(plan);
        dbContext.WorkoutPlanDays.Add(new Models.WorkoutPlanDay
        {
            Id = Guid.NewGuid(),
            WorkoutPlanId = plan.Id,
            DayOfWeek = 3,
            Note = "Unreachable"
        });
        await dbContext.SaveChangesAsync();

        var result = await new WorkoutPlanRepository(dbContext).GetTodaysPlanDayAsync(userId, 3);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetTodaysPlanDayAsync_WithNoDayScheduled_ReturnsNull()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var userId = Guid.NewGuid();
        var plan = MakePlan(userId, isActive: true);
        dbContext.WorkoutPlans.Add(plan);
        dbContext.WorkoutPlanDays.Add(new Models.WorkoutPlanDay { Id = Guid.NewGuid(), WorkoutPlanId = plan.Id, DayOfWeek = 1 });
        await dbContext.SaveChangesAsync();

        var result = await new WorkoutPlanRepository(dbContext).GetTodaysPlanDayAsync(userId, 6);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetTodaysPlanDayAsync_IgnoresAnotherUsersActivePlan()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var plan = MakePlan(Guid.NewGuid(), isActive: true);
        dbContext.WorkoutPlans.Add(plan);
        dbContext.WorkoutPlanDays.Add(new Models.WorkoutPlanDay { Id = Guid.NewGuid(), WorkoutPlanId = plan.Id, DayOfWeek = 2 });
        await dbContext.SaveChangesAsync();

        var result = await new WorkoutPlanRepository(dbContext).GetTodaysPlanDayAsync(Guid.NewGuid(), 2);

        result.Should().BeNull();
    }
}
