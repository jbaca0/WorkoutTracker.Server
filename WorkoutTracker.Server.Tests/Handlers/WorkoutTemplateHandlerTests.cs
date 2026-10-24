using FluentAssertions;
using Microsoft.AspNetCore.Http.HttpResults;
using WorkoutTracker.Server.DataTransferObjects.Requests.WorkoutTemplate;
using WorkoutTracker.Server.DataTransferObjects.Responses.WorkoutTemplate;
using WorkoutTracker.Server.Handlers;
using WorkoutTracker.Server.Models.Repositories;
using WorkoutTracker.Server.Services;
using WorkoutTracker.Server.Tests.Helpers;
using Xunit;

namespace WorkoutTracker.Server.Tests.Handlers;

public class WorkoutTemplateHandlerTests
{
    private static Models.ExerciseInfo MakeExerciseInfo(string name = "Bench Press") =>
        new() { Id = Guid.NewGuid(), Name = name, SecondaryMuscleGroupsJson = "[]" };

    private static UserExerciseInfoService MakeUserExerciseInfoService(Models.ApplicationDbContext dbContext) =>
        new(
            new ExerciseRepository(dbContext),
            new ExerciseGoalRepository(dbContext),
            new WorkoutPlanRepository(dbContext));

    private static WorkoutTemplateExerciseRequest ExerciseRequest(Guid exerciseInfoId, params int[] reps) =>
        new()
        {
            exerciseInfoId = exerciseInfoId,
            sets = reps.Select(r => new WorkoutTemplateExerciseSetRequest { reps = r }).ToList()
        };

    private static Models.WorkoutTemplate MakeTemplate(Guid userId, string name = "Template") =>
        new() { Id = Guid.NewGuid(), Name = name, UserId = userId };

    /// <summary>Adds a template owned by <paramref name="userId"/> with one exercise and the given set reps.</summary>
    private static async Task<Models.WorkoutTemplate> SeedTemplateAsync(
        Models.ApplicationDbContext dbContext, Guid userId, Guid exerciseInfoId, params int[] reps)
    {
        var template = MakeTemplate(userId);
        var exerciseId = Guid.NewGuid();
        template.Exercises.Add(new Models.WorkoutTemplateExercise
        {
            Id = exerciseId,
            WorkoutTemplateId = template.Id,
            ExerciseInfoId = exerciseInfoId,
            Order = 0,
            Sets = reps.Select((r, i) => new Models.WorkoutTemplateExerciseSet
            {
                Id = Guid.NewGuid(),
                WorkoutTemplateExerciseId = exerciseId,
                Order = i,
                Reps = r
            }).ToList()
        });
        dbContext.WorkoutTemplates.Add(template);
        await dbContext.SaveChangesAsync();
        return template;
    }

    // --- GetTemplates ---

    [Fact]
    public async Task GetTemplates_ReturnsOnlyTheCurrentUsersTemplates()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var userId = Guid.NewGuid();
        dbContext.WorkoutTemplates.AddRange(MakeTemplate(userId, "Mine"), MakeTemplate(Guid.NewGuid(), "Theirs"));
        await dbContext.SaveChangesAsync();

        var result = await WorkoutTemplateHandler.GetTemplates(
            ClaimsPrincipalBuilder.WithUserId(userId),
            new WorkoutTemplateRepository(dbContext));

        var ok = result.Should().BeOfType<Ok<GetTemplatesResponse>>().Subject;
        ok.Value!.templates.Should().ContainSingle().Which.name.Should().Be("Mine");
    }

    [Fact]
    public async Task GetTemplates_WithNoTemplates_ReturnsAnEmptyList()
    {
        var dbContext = DbContextFactory.CreateInMemory();

        var result = await WorkoutTemplateHandler.GetTemplates(
            ClaimsPrincipalBuilder.WithUserId(Guid.NewGuid()),
            new WorkoutTemplateRepository(dbContext));

        var ok = result.Should().BeOfType<Ok<GetTemplatesResponse>>().Subject;
        ok.Value!.templates.Should().BeEmpty();
    }

    // --- AddTemplate ---

    [Fact]
    public async Task AddTemplate_PersistsTheTemplateForTheUserAndReturnsItsId()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var userId = Guid.NewGuid();
        var exerciseInfo = MakeExerciseInfo();
        dbContext.ExerciseInfos.Add(exerciseInfo);
        await dbContext.SaveChangesAsync();

        var request = new CreateTemplateRequest
        {
            name = "Upper body",
            exercises = new List<WorkoutTemplateExerciseRequest> { ExerciseRequest(exerciseInfo.Id, 10, 8) }
        };

        var result = await WorkoutTemplateHandler.AddTemplate(
            request, ClaimsPrincipalBuilder.WithUserId(userId), new WorkoutTemplateRepository(dbContext));

        var ok = result.Should().BeOfType<Ok<Guid>>().Subject;
        var saved = dbContext.WorkoutTemplates.Should().ContainSingle().Subject;
        saved.Id.Should().Be(ok.Value);
        saved.Name.Should().Be("Upper body");
        saved.UserId.Should().Be(userId);
    }

    // --- UpdateTemplate ---

    [Fact]
    public async Task UpdateTemplate_WithUnknownTemplate_ReturnsNotFound()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var request = new UpdateTemplateRequest { templateId = Guid.NewGuid(), name = "Nope" };

        var result = await WorkoutTemplateHandler.UpdateTemplate(
            request, ClaimsPrincipalBuilder.WithUserId(Guid.NewGuid()), new WorkoutTemplateRepository(dbContext));

        result.Should().BeOfType<NotFound>();
    }

    [Fact]
    public async Task UpdateTemplate_WithAnotherUsersTemplate_ReturnsForbid()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var template = MakeTemplate(Guid.NewGuid());
        dbContext.WorkoutTemplates.Add(template);
        await dbContext.SaveChangesAsync();
        var request = new UpdateTemplateRequest { templateId = template.Id, name = "Hijacked" };

        var result = await WorkoutTemplateHandler.UpdateTemplate(
            request, ClaimsPrincipalBuilder.WithUserId(Guid.NewGuid()), new WorkoutTemplateRepository(dbContext));

        result.Should().BeOfType<ForbidHttpResult>();
        dbContext.WorkoutTemplates.Single(t => t.Id == template.Id).Name.Should().Be("Template");
    }

    [Fact]
    public async Task UpdateTemplate_WithOwnTemplate_ReturnsOkAndAppliesTheChanges()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var userId = Guid.NewGuid();
        var exerciseInfo = MakeExerciseInfo();
        dbContext.ExerciseInfos.Add(exerciseInfo);
        var template = await SeedTemplateAsync(dbContext, userId, exerciseInfo.Id, 10);

        var request = new UpdateTemplateRequest
        {
            templateId = template.Id,
            name = "Renamed",
            exercises = new List<WorkoutTemplateExerciseRequest> { ExerciseRequest(exerciseInfo.Id, 5, 5) }
        };

        var result = await WorkoutTemplateHandler.UpdateTemplate(
            request, ClaimsPrincipalBuilder.WithUserId(userId), new WorkoutTemplateRepository(dbContext));

        result.Should().BeOfType<Ok>();
        var saved = await new WorkoutTemplateRepository(dbContext).GetByIdAsync(template.Id);
        saved!.Name.Should().Be("Renamed");
        saved.Exercises.Should().ContainSingle().Which.Sets.Should().HaveCount(2);
    }

    // --- DeleteTemplate ---

    [Fact]
    public async Task DeleteTemplate_WithUnknownTemplate_ReturnsNotFound()
    {
        var dbContext = DbContextFactory.CreateInMemory();

        var result = await WorkoutTemplateHandler.DeleteTemplate(
            Guid.NewGuid(), ClaimsPrincipalBuilder.WithUserId(Guid.NewGuid()), new WorkoutTemplateRepository(dbContext));

        result.Should().BeOfType<NotFound>();
    }

    [Fact]
    public async Task DeleteTemplate_WithAnotherUsersTemplate_ReturnsForbidAndKeepsIt()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var template = MakeTemplate(Guid.NewGuid());
        dbContext.WorkoutTemplates.Add(template);
        await dbContext.SaveChangesAsync();

        var result = await WorkoutTemplateHandler.DeleteTemplate(
            template.Id, ClaimsPrincipalBuilder.WithUserId(Guid.NewGuid()), new WorkoutTemplateRepository(dbContext));

        result.Should().BeOfType<ForbidHttpResult>();
        dbContext.WorkoutTemplates.Should().ContainSingle();
    }

    [Fact]
    public async Task DeleteTemplate_WithOwnTemplate_ReturnsOkAndRemovesIt()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var userId = Guid.NewGuid();
        var template = MakeTemplate(userId);
        dbContext.WorkoutTemplates.Add(template);
        await dbContext.SaveChangesAsync();

        var result = await WorkoutTemplateHandler.DeleteTemplate(
            template.Id, ClaimsPrincipalBuilder.WithUserId(userId), new WorkoutTemplateRepository(dbContext));

        result.Should().BeOfType<Ok>();
        dbContext.WorkoutTemplates.Should().BeEmpty();
    }

    // --- StartWorkoutFromTemplate ---

    [Fact]
    public async Task StartWorkoutFromTemplate_WithUnknownTemplate_ReturnsNotFound()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var request = new StartWorkoutFromTemplateRequest { templateId = Guid.NewGuid() };

        var result = await WorkoutTemplateHandler.StartWorkoutFromTemplate(
            request,
            ClaimsPrincipalBuilder.WithUserId(Guid.NewGuid()),
            new WorkoutTemplateRepository(dbContext),
            dbContext,
            MakeUserExerciseInfoService(dbContext));

        result.Should().BeOfType<NotFound>();
        dbContext.Workouts.Should().BeEmpty();
    }

    [Fact]
    public async Task StartWorkoutFromTemplate_WithAnotherUsersTemplate_ReturnsForbid()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var exerciseInfo = MakeExerciseInfo();
        dbContext.ExerciseInfos.Add(exerciseInfo);
        var template = await SeedTemplateAsync(dbContext, Guid.NewGuid(), exerciseInfo.Id, 10);
        var request = new StartWorkoutFromTemplateRequest { templateId = template.Id };

        var result = await WorkoutTemplateHandler.StartWorkoutFromTemplate(
            request,
            ClaimsPrincipalBuilder.WithUserId(Guid.NewGuid()),
            new WorkoutTemplateRepository(dbContext),
            dbContext,
            MakeUserExerciseInfoService(dbContext));

        result.Should().BeOfType<ForbidHttpResult>();
        dbContext.Workouts.Should().BeEmpty();
    }

    [Fact]
    public async Task StartWorkoutFromTemplate_CreatesAWorkoutOwnedByTheUser()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var userId = Guid.NewGuid();
        var exerciseInfo = MakeExerciseInfo();
        dbContext.ExerciseInfos.Add(exerciseInfo);
        var template = await SeedTemplateAsync(dbContext, userId, exerciseInfo.Id, 10);
        var request = new StartWorkoutFromTemplateRequest { templateId = template.Id };

        var result = await WorkoutTemplateHandler.StartWorkoutFromTemplate(
            request,
            ClaimsPrincipalBuilder.WithUserId(userId),
            new WorkoutTemplateRepository(dbContext),
            dbContext,
            MakeUserExerciseInfoService(dbContext));

        var ok = result.Should().BeOfType<Ok<StartWorkoutFromTemplateResponse>>().Subject;
        var workout = dbContext.Workouts.Should().ContainSingle().Subject;
        workout.UserId.Should().Be(userId);
        ok.Value!.workoutId.Should().Be(workout.Id);
    }

    [Fact]
    public async Task StartWorkoutFromTemplate_CopiesTemplateRepsIntoBlankUncompletedSets()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var userId = Guid.NewGuid();
        var exerciseInfo = MakeExerciseInfo();
        dbContext.ExerciseInfos.Add(exerciseInfo);
        var template = await SeedTemplateAsync(dbContext, userId, exerciseInfo.Id, 12, 10);
        var request = new StartWorkoutFromTemplateRequest { templateId = template.Id };

        await WorkoutTemplateHandler.StartWorkoutFromTemplate(
            request,
            ClaimsPrincipalBuilder.WithUserId(userId),
            new WorkoutTemplateRepository(dbContext),
            dbContext,
            MakeUserExerciseInfoService(dbContext));

        var exercise = dbContext.Exercises.Should().ContainSingle().Subject;
        var sets = dbContext.Sets.Where(s => s.ExerciseId == exercise.Id).OrderBy(s => s.Order).ToList();
        sets.Select(s => s.Reps).Should().Equal(12, 10);
        sets.Should().OnlyContain(s => s.Weight == 0m);
        sets.Should().OnlyContain(s => !s.Completed);
        // Sets copied from a template are numbered from zero.
        sets.Select(s => s.Order).Should().Equal(0, 1);
    }

    [Fact]
    public async Task StartWorkoutFromTemplate_AddsExercisesInTemplateOrder()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var userId = Guid.NewGuid();
        var first = MakeExerciseInfo("First");
        var second = MakeExerciseInfo("Second");
        dbContext.ExerciseInfos.AddRange(first, second);

        var template = MakeTemplate(userId);
        template.Exercises.Add(new Models.WorkoutTemplateExercise
        {
            Id = Guid.NewGuid(), WorkoutTemplateId = template.Id, ExerciseInfoId = second.Id, Order = 1
        });
        template.Exercises.Add(new Models.WorkoutTemplateExercise
        {
            Id = Guid.NewGuid(), WorkoutTemplateId = template.Id, ExerciseInfoId = first.Id, Order = 0
        });
        dbContext.WorkoutTemplates.Add(template);
        await dbContext.SaveChangesAsync();

        await WorkoutTemplateHandler.StartWorkoutFromTemplate(
            new StartWorkoutFromTemplateRequest { templateId = template.Id },
            ClaimsPrincipalBuilder.WithUserId(userId),
            new WorkoutTemplateRepository(dbContext),
            dbContext,
            MakeUserExerciseInfoService(dbContext));

        dbContext.Exercises.Should().HaveCount(2);
    }

    [Fact]
    public async Task StartWorkoutFromTemplate_ReturnsUserExerciseInfoForEachTemplateExercise()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var userId = Guid.NewGuid();
        var exerciseInfo = MakeExerciseInfo("Deadlift");
        dbContext.ExerciseInfos.Add(exerciseInfo);
        var template = await SeedTemplateAsync(dbContext, userId, exerciseInfo.Id, 5);

        var result = await WorkoutTemplateHandler.StartWorkoutFromTemplate(
            new StartWorkoutFromTemplateRequest { templateId = template.Id },
            ClaimsPrincipalBuilder.WithUserId(userId),
            new WorkoutTemplateRepository(dbContext),
            dbContext,
            MakeUserExerciseInfoService(dbContext));

        var ok = result.Should().BeOfType<Ok<StartWorkoutFromTemplateResponse>>().Subject;
        var info = ok.Value!.userExerciseInfos.Should().ContainSingle().Subject;
        info.exerciseInfo.name.Should().Be("Deadlift");
    }

    [Fact]
    public async Task StartWorkoutFromTemplate_TargetsThePreviousWorkoutsSetsWhenThereIsHistory()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var userId = Guid.NewGuid();
        var exerciseInfo = MakeExerciseInfo();
        dbContext.ExerciseInfos.Add(exerciseInfo);

        var previousWorkout = new Models.Workout { Id = Guid.NewGuid(), UserId = userId, AddDateTime = DateTime.UtcNow.AddDays(-2) };
        var previousExercise = new Models.Exercise
        {
            Id = Guid.NewGuid(),
            WorkoutId = previousWorkout.Id,
            ExerciseInfoId = exerciseInfo.Id
        };
        previousExercise.Sets.Add(new Models.Set
        {
            Id = Guid.NewGuid(),
            ExerciseId = previousExercise.Id,
            Reps = 6,
            Weight = 225m,
            Order = 1
        });
        dbContext.Workouts.Add(previousWorkout);
        dbContext.Exercises.Add(previousExercise);
        var template = await SeedTemplateAsync(dbContext, userId, exerciseInfo.Id, 5);

        var result = await WorkoutTemplateHandler.StartWorkoutFromTemplate(
            new StartWorkoutFromTemplateRequest { templateId = template.Id },
            ClaimsPrincipalBuilder.WithUserId(userId),
            new WorkoutTemplateRepository(dbContext),
            dbContext,
            MakeUserExerciseInfoService(dbContext));

        var ok = result.Should().BeOfType<Ok<StartWorkoutFromTemplateResponse>>().Subject;
        var info = ok.Value!.userExerciseInfos.Should().ContainSingle().Subject;
        info.target.Should().NotBeNull();
        info.target!.sets.Should().ContainSingle();
        info.target.sets[0].reps.Should().Be(6);
        info.target.sets[0].weight.Should().Be(225m);
        info.bestSetWeight.Should().Be(225m);
        info.bestSetReps.Should().Be(6);
    }

    [Fact]
    public async Task StartWorkoutFromTemplate_WithNoHistory_ReturnsNoTarget()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var userId = Guid.NewGuid();
        var exerciseInfo = MakeExerciseInfo();
        dbContext.ExerciseInfos.Add(exerciseInfo);
        var template = await SeedTemplateAsync(dbContext, userId, exerciseInfo.Id, 5);

        var result = await WorkoutTemplateHandler.StartWorkoutFromTemplate(
            new StartWorkoutFromTemplateRequest { templateId = template.Id },
            ClaimsPrincipalBuilder.WithUserId(userId),
            new WorkoutTemplateRepository(dbContext),
            dbContext,
            MakeUserExerciseInfoService(dbContext));

        var ok = result.Should().BeOfType<Ok<StartWorkoutFromTemplateResponse>>().Subject;
        var info = ok.Value!.userExerciseInfos.Should().ContainSingle().Subject;
        info.target.Should().BeNull();
    }

    // Failing on purpose: see jbaca0/WorkoutTracker.Server#15.
    [Fact]
    public async Task StartWorkoutFromTemplate_WithNoHistory_ReportsNoBestSet()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var userId = Guid.NewGuid();
        var exerciseInfo = MakeExerciseInfo();
        dbContext.ExerciseInfos.Add(exerciseInfo);
        var template = await SeedTemplateAsync(dbContext, userId, exerciseInfo.Id, 5);

        var result = await WorkoutTemplateHandler.StartWorkoutFromTemplate(
            new StartWorkoutFromTemplateRequest { templateId = template.Id },
            ClaimsPrincipalBuilder.WithUserId(userId),
            new WorkoutTemplateRepository(dbContext),
            dbContext,
            MakeUserExerciseInfoService(dbContext));

        var ok = result.Should().BeOfType<Ok<StartWorkoutFromTemplateResponse>>().Subject;
        var info = ok.Value!.userExerciseInfos.Should().ContainSingle().Subject;

        // The user has never performed this exercise, so there is no best set to report.
        // The handler saves the new workout's blank Weight = 0 sets before asking for one,
        // and GetBestSetByEpleyAsync takes no excludeWorkoutId the way
        // GetLatestExercisesForUserAsync does, so it counts the sets it just created and
        // answers 0 x 5 reps.
        info.bestSetWeight.Should().BeNull();
        info.bestSetReps.Should().BeNull();
    }
}
