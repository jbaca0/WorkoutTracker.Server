using FluentAssertions;
using Microsoft.AspNetCore.Http.HttpResults;
using WorkoutTracker.Server.DataTransferObjects.Requests.WorkoutPlan;
using WorkoutTracker.Server.DataTransferObjects.Responses.WorkoutPlan;
using WorkoutTracker.Server.Handlers;
using WorkoutTracker.Server.Models.Repositories;
using WorkoutTracker.Server.Tests.Helpers;
using Xunit;

namespace WorkoutTracker.Server.Tests.Handlers;

public class WorkoutPlanHandlerTests
{
    private static Models.WorkoutPlan MakePlan(Guid userId, string name = "Plan", bool isActive = false) =>
        new()
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = name,
            IsActive = isActive,
            StartDate = new DateTime(2026, 1, 1),
            CreatedDateTime = new DateTime(2026, 1, 1)
        };

    private static WorkoutPlanDayRequest DayRequest(int dayOfWeek, Guid? templateId = null, string? note = null) =>
        new() { dayOfWeek = dayOfWeek, workoutTemplateId = templateId, note = note };

    // --- GetWorkoutPlans ---

    [Fact]
    public async Task GetWorkoutPlans_ReturnsOnlyTheCurrentUsersPlans()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var userId = Guid.NewGuid();
        dbContext.WorkoutPlans.AddRange(MakePlan(userId, "Mine"), MakePlan(Guid.NewGuid(), "Theirs"));
        await dbContext.SaveChangesAsync();

        var result = await WorkoutPlanHandler.GetWorkoutPlans(
            ClaimsPrincipalBuilder.WithUserId(userId), new WorkoutPlanRepository(dbContext));

        var ok = result.Should().BeOfType<Ok<GetWorkoutPlansResponse>>().Subject;
        ok.Value!.plans.Should().ContainSingle().Which.name.Should().Be("Mine");
    }

    [Fact]
    public async Task GetWorkoutPlans_WithNoPlans_ReturnsAnEmptyList()
    {
        var dbContext = DbContextFactory.CreateInMemory();

        var result = await WorkoutPlanHandler.GetWorkoutPlans(
            ClaimsPrincipalBuilder.WithUserId(Guid.NewGuid()), new WorkoutPlanRepository(dbContext));

        var ok = result.Should().BeOfType<Ok<GetWorkoutPlansResponse>>().Subject;
        ok.Value!.plans.Should().BeEmpty();
    }

    // --- AddWorkoutPlan ---

    [Fact]
    public async Task AddWorkoutPlan_PersistsThePlanAndReturnsItsId()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var userId = Guid.NewGuid();
        var request = new AddWorkoutPlanRequest
        {
            name = "Summer block",
            startDate = new DateTime(2026, 6, 1),
            endDate = new DateTime(2026, 8, 1),
            days = new List<WorkoutPlanDayRequest> { DayRequest(1, note: "Heavy") }
        };

        var result = await WorkoutPlanHandler.AddWorkoutPlan(
            request, ClaimsPrincipalBuilder.WithUserId(userId), new WorkoutPlanRepository(dbContext));

        var ok = result.Should().BeOfType<Ok<Guid>>().Subject;
        var plan = dbContext.WorkoutPlans.Should().ContainSingle().Subject;
        plan.Id.Should().Be(ok.Value);
        plan.Name.Should().Be("Summer block");
        plan.UserId.Should().Be(userId);
        dbContext.WorkoutPlanDays.Should().ContainSingle().Which.Note.Should().Be("Heavy");
    }

    // --- UpdateWorkoutPlan ---

    [Fact]
    public async Task UpdateWorkoutPlan_WithUnknownPlan_ReturnsNotFound()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var request = new UpdateWorkoutPlanRequest { planId = Guid.NewGuid(), name = "Nope" };

        var result = await WorkoutPlanHandler.UpdateWorkoutPlan(
            request, ClaimsPrincipalBuilder.WithUserId(Guid.NewGuid()), new WorkoutPlanRepository(dbContext));

        result.Should().BeOfType<NotFound>();
    }

    [Fact]
    public async Task UpdateWorkoutPlan_WithAnotherUsersPlan_ReturnsForbidAndChangesNothing()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var plan = MakePlan(Guid.NewGuid());
        dbContext.WorkoutPlans.Add(plan);
        await dbContext.SaveChangesAsync();
        var request = new UpdateWorkoutPlanRequest { planId = plan.Id, name = "Hijacked", startDate = new DateTime(2026, 1, 1) };

        var result = await WorkoutPlanHandler.UpdateWorkoutPlan(
            request, ClaimsPrincipalBuilder.WithUserId(Guid.NewGuid()), new WorkoutPlanRepository(dbContext));

        result.Should().BeOfType<ForbidHttpResult>();
        dbContext.WorkoutPlans.Single(p => p.Id == plan.Id).Name.Should().Be("Plan");
    }

    [Fact]
    public async Task UpdateWorkoutPlan_WithOwnPlan_ReturnsOkAndAppliesTheChanges()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var userId = Guid.NewGuid();
        var plan = MakePlan(userId);
        dbContext.WorkoutPlans.Add(plan);
        await dbContext.SaveChangesAsync();

        var request = new UpdateWorkoutPlanRequest
        {
            planId = plan.Id,
            name = "Renamed",
            startDate = new DateTime(2026, 5, 1),
            days = new List<WorkoutPlanDayRequest> { DayRequest(2, note: "Tempo") }
        };

        var result = await WorkoutPlanHandler.UpdateWorkoutPlan(
            request, ClaimsPrincipalBuilder.WithUserId(userId), new WorkoutPlanRepository(dbContext));

        result.Should().BeOfType<Ok>();
        var saved = dbContext.WorkoutPlans.Single(p => p.Id == plan.Id);
        saved.Name.Should().Be("Renamed");
        saved.StartDate.Should().Be(new DateTime(2026, 5, 1));
        dbContext.WorkoutPlanDays.Should().ContainSingle().Which.Note.Should().Be("Tempo");
    }

    // --- DeleteWorkoutPlan ---

    [Fact]
    public async Task DeleteWorkoutPlan_WithUnknownPlan_ReturnsNotFound()
    {
        var dbContext = DbContextFactory.CreateInMemory();

        var result = await WorkoutPlanHandler.DeleteWorkoutPlan(
            Guid.NewGuid(), ClaimsPrincipalBuilder.WithUserId(Guid.NewGuid()), new WorkoutPlanRepository(dbContext));

        result.Should().BeOfType<NotFound>();
    }

    [Fact]
    public async Task DeleteWorkoutPlan_WithAnotherUsersPlan_ReturnsForbidAndKeepsIt()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var plan = MakePlan(Guid.NewGuid());
        dbContext.WorkoutPlans.Add(plan);
        await dbContext.SaveChangesAsync();

        var result = await WorkoutPlanHandler.DeleteWorkoutPlan(
            plan.Id, ClaimsPrincipalBuilder.WithUserId(Guid.NewGuid()), new WorkoutPlanRepository(dbContext));

        result.Should().BeOfType<ForbidHttpResult>();
        dbContext.WorkoutPlans.Should().ContainSingle();
    }

    [Fact]
    public async Task DeleteWorkoutPlan_WithOwnPlan_ReturnsOkAndRemovesIt()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var userId = Guid.NewGuid();
        var plan = MakePlan(userId);
        dbContext.WorkoutPlans.Add(plan);
        await dbContext.SaveChangesAsync();

        var result = await WorkoutPlanHandler.DeleteWorkoutPlan(
            plan.Id, ClaimsPrincipalBuilder.WithUserId(userId), new WorkoutPlanRepository(dbContext));

        result.Should().BeOfType<Ok>();
        dbContext.WorkoutPlans.Should().BeEmpty();
    }

    // --- ActivateWorkoutPlan ---

    [Fact]
    public async Task ActivateWorkoutPlan_WithUnknownPlan_ReturnsNotFound()
    {
        var dbContext = DbContextFactory.CreateInMemory();

        var result = await WorkoutPlanHandler.ActivateWorkoutPlan(
            Guid.NewGuid(), ClaimsPrincipalBuilder.WithUserId(Guid.NewGuid()), new WorkoutPlanRepository(dbContext));

        result.Should().BeOfType<NotFound>();
    }

    [Fact]
    public async Task ActivateWorkoutPlan_WithAnotherUsersPlan_ReturnsForbid()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var plan = MakePlan(Guid.NewGuid());
        dbContext.WorkoutPlans.Add(plan);
        await dbContext.SaveChangesAsync();

        var result = await WorkoutPlanHandler.ActivateWorkoutPlan(
            plan.Id, ClaimsPrincipalBuilder.WithUserId(Guid.NewGuid()), new WorkoutPlanRepository(dbContext));

        result.Should().BeOfType<ForbidHttpResult>();
        dbContext.WorkoutPlans.Single(p => p.Id == plan.Id).IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task ActivateWorkoutPlan_WithOwnPlan_ActivatesItAndDeactivatesTheOthers()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var userId = Guid.NewGuid();
        var current = MakePlan(userId, "Current", isActive: true);
        var wanted = MakePlan(userId, "Wanted");
        dbContext.WorkoutPlans.AddRange(current, wanted);
        await dbContext.SaveChangesAsync();

        var result = await WorkoutPlanHandler.ActivateWorkoutPlan(
            wanted.Id, ClaimsPrincipalBuilder.WithUserId(userId), new WorkoutPlanRepository(dbContext));

        result.Should().BeOfType<Ok>();
        dbContext.WorkoutPlans.Single(p => p.Id == wanted.Id).IsActive.Should().BeTrue();
        dbContext.WorkoutPlans.Single(p => p.Id == current.Id).IsActive.Should().BeFalse();
    }

    // --- GetTodaysWorkout ---

    [Fact]
    public async Task GetTodaysWorkout_ForTheGivenDay_ReturnsTheScheduledTemplateAndNote()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var userId = Guid.NewGuid();
        var template = new Models.WorkoutTemplate { Id = Guid.NewGuid(), Name = "Push day", UserId = userId };
        var plan = MakePlan(userId, isActive: true);
        dbContext.WorkoutTemplates.Add(template);
        dbContext.WorkoutPlans.Add(plan);
        dbContext.WorkoutPlanDays.Add(new Models.WorkoutPlanDay
        {
            Id = Guid.NewGuid(),
            WorkoutPlanId = plan.Id,
            DayOfWeek = 2,
            WorkoutTemplateId = template.Id,
            Note = "Go heavy"
        });
        await dbContext.SaveChangesAsync();

        var result = await WorkoutPlanHandler.GetTodaysWorkout(
            2, ClaimsPrincipalBuilder.WithUserId(userId), new WorkoutPlanRepository(dbContext));

        var ok = result.Should().BeOfType<Ok<GetTodaysWorkoutResponse>>().Subject;
        ok.Value!.template!.name.Should().Be("Push day");
        ok.Value.note.Should().Be("Go heavy");
    }

    [Fact]
    public async Task GetTodaysWorkout_WithNothingScheduled_ReturnsAnEmptyResponse()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var userId = Guid.NewGuid();
        var plan = MakePlan(userId, isActive: true);
        dbContext.WorkoutPlans.Add(plan);
        await dbContext.SaveChangesAsync();

        var result = await WorkoutPlanHandler.GetTodaysWorkout(
            4, ClaimsPrincipalBuilder.WithUserId(userId), new WorkoutPlanRepository(dbContext));

        var ok = result.Should().BeOfType<Ok<GetTodaysWorkoutResponse>>().Subject;
        ok.Value!.template.Should().BeNull();
        ok.Value.note.Should().BeNull();
    }

    [Fact]
    public async Task GetTodaysWorkout_WithARestDay_ReturnsTheNoteWithoutATemplate()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var userId = Guid.NewGuid();
        var plan = MakePlan(userId, isActive: true);
        dbContext.WorkoutPlans.Add(plan);
        dbContext.WorkoutPlanDays.Add(new Models.WorkoutPlanDay
        {
            Id = Guid.NewGuid(),
            WorkoutPlanId = plan.Id,
            DayOfWeek = 0,
            Note = "Rest"
        });
        await dbContext.SaveChangesAsync();

        var result = await WorkoutPlanHandler.GetTodaysWorkout(
            0, ClaimsPrincipalBuilder.WithUserId(userId), new WorkoutPlanRepository(dbContext));

        var ok = result.Should().BeOfType<Ok<GetTodaysWorkoutResponse>>().Subject;
        ok.Value!.template.Should().BeNull();
        ok.Value.note.Should().Be("Rest");
    }

    [Fact]
    public async Task GetTodaysWorkout_WithNoDayOfWeekGiven_FallsBackToToday()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var userId = Guid.NewGuid();
        var template = new Models.WorkoutTemplate { Id = Guid.NewGuid(), Name = "Today's session", UserId = userId };
        var plan = MakePlan(userId, isActive: true);
        dbContext.WorkoutTemplates.Add(template);
        dbContext.WorkoutPlans.Add(plan);
        dbContext.WorkoutPlanDays.Add(new Models.WorkoutPlanDay
        {
            Id = Guid.NewGuid(),
            WorkoutPlanId = plan.Id,
            DayOfWeek = (int)DateTime.UtcNow.DayOfWeek,
            WorkoutTemplateId = template.Id
        });
        await dbContext.SaveChangesAsync();

        var result = await WorkoutPlanHandler.GetTodaysWorkout(
            null, ClaimsPrincipalBuilder.WithUserId(userId), new WorkoutPlanRepository(dbContext));

        var ok = result.Should().BeOfType<Ok<GetTodaysWorkoutResponse>>().Subject;
        ok.Value!.template!.name.Should().Be("Today's session");
    }

    [Fact]
    public async Task GetTodaysWorkout_IgnoresAnInactivePlan()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var userId = Guid.NewGuid();
        var template = new Models.WorkoutTemplate { Id = Guid.NewGuid(), Name = "Shelved", UserId = userId };
        var plan = MakePlan(userId, isActive: false);
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

        var result = await WorkoutPlanHandler.GetTodaysWorkout(
            3, ClaimsPrincipalBuilder.WithUserId(userId), new WorkoutPlanRepository(dbContext));

        var ok = result.Should().BeOfType<Ok<GetTodaysWorkoutResponse>>().Subject;
        ok.Value!.template.Should().BeNull();
    }
}
