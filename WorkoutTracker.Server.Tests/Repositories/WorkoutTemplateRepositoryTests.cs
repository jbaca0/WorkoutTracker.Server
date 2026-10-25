using FluentAssertions;
using WorkoutTracker.Server.DataTransferObjects.Requests.WorkoutTemplate;
using WorkoutTracker.Server.Models.Repositories;
using WorkoutTracker.Server.Tests.Helpers;
using Xunit;

namespace WorkoutTracker.Server.Tests.Repositories;

public class WorkoutTemplateRepositoryTests
{
    private static Models.ExerciseInfo MakeExerciseInfo(string name = "Bench Press") =>
        new() { Id = Guid.NewGuid(), Name = name, SecondaryMuscleGroupsJson = "[]" };

    private static WorkoutTemplateExerciseRequest ExerciseRequest(Guid exerciseInfoId, params int[] reps) =>
        new()
        {
            exerciseInfoId = exerciseInfoId,
            sets = reps.Select(r => new WorkoutTemplateExerciseSetRequest { reps = r }).ToList()
        };

    // --- GetAllForUserAsync ---

    [Fact]
    public async Task GetAllForUserAsync_ReturnsOnlyTemplatesOwnedByTheUser()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var userId = Guid.NewGuid();
        dbContext.WorkoutTemplates.AddRange(
            new Models.WorkoutTemplate { Id = Guid.NewGuid(), Name = "Mine", UserId = userId },
            new Models.WorkoutTemplate { Id = Guid.NewGuid(), Name = "Someone else's", UserId = Guid.NewGuid() });
        await dbContext.SaveChangesAsync();

        var result = await new WorkoutTemplateRepository(dbContext).GetAllForUserAsync(userId);

        result.Should().ContainSingle();
        result[0].Name.Should().Be("Mine");
    }

    [Fact]
    public async Task GetAllForUserAsync_OrdersByCreatedDateTime()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var userId = Guid.NewGuid();
        dbContext.WorkoutTemplates.AddRange(
            new Models.WorkoutTemplate { Id = Guid.NewGuid(), Name = "Second", UserId = userId, CreatedDateTime = new DateTime(2026, 2, 1) },
            new Models.WorkoutTemplate { Id = Guid.NewGuid(), Name = "First", UserId = userId, CreatedDateTime = new DateTime(2026, 1, 1) });
        await dbContext.SaveChangesAsync();

        var result = await new WorkoutTemplateRepository(dbContext).GetAllForUserAsync(userId);

        result.Select(t => t.Name).Should().Equal("First", "Second");
    }

    [Fact]
    public async Task GetAllForUserAsync_IncludesExercisesWithTheirExerciseInfoAndSets()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var userId = Guid.NewGuid();
        var exerciseInfo = MakeExerciseInfo("Squat");
        var template = new Models.WorkoutTemplate { Id = Guid.NewGuid(), Name = "Leg day", UserId = userId };
        var exerciseId = Guid.NewGuid();
        template.Exercises.Add(new Models.WorkoutTemplateExercise
        {
            Id = exerciseId,
            WorkoutTemplateId = template.Id,
            ExerciseInfoId = exerciseInfo.Id,
            Order = 0,
            Sets = new List<Models.WorkoutTemplateExerciseSet>
            {
                new() { Id = Guid.NewGuid(), WorkoutTemplateExerciseId = exerciseId, Order = 0, Reps = 5 },
                new() { Id = Guid.NewGuid(), WorkoutTemplateExerciseId = exerciseId, Order = 1, Reps = 3 }
            }
        });
        dbContext.ExerciseInfos.Add(exerciseInfo);
        dbContext.WorkoutTemplates.Add(template);
        await dbContext.SaveChangesAsync();

        var result = await new WorkoutTemplateRepository(dbContext).GetAllForUserAsync(userId);

        var exercise = result.Should().ContainSingle().Subject.Exercises.Should().ContainSingle().Subject;
        exercise.ExerciseInfo!.Name.Should().Be("Squat");
        exercise.Sets.Select(s => s.Reps).Should().Equal(5, 3);
    }

    [Fact]
    public async Task GetAllForUserAsync_WithNoTemplates_ReturnsEmptyList()
    {
        var dbContext = DbContextFactory.CreateInMemory();

        var result = await new WorkoutTemplateRepository(dbContext).GetAllForUserAsync(Guid.NewGuid());

        result.Should().BeEmpty();
    }

    // --- GetByIdAsync ---

    [Fact]
    public async Task GetByIdAsync_WithExistingTemplate_ReturnsItWithExercisesAndSets()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var exerciseInfo = MakeExerciseInfo();
        var template = new Models.WorkoutTemplate { Id = Guid.NewGuid(), Name = "Push", UserId = Guid.NewGuid() };
        var exerciseId = Guid.NewGuid();
        template.Exercises.Add(new Models.WorkoutTemplateExercise
        {
            Id = exerciseId,
            WorkoutTemplateId = template.Id,
            ExerciseInfoId = exerciseInfo.Id,
            Sets = new List<Models.WorkoutTemplateExerciseSet>
            {
                new() { Id = Guid.NewGuid(), WorkoutTemplateExerciseId = exerciseId, Order = 0, Reps = 8 }
            }
        });
        dbContext.ExerciseInfos.Add(exerciseInfo);
        dbContext.WorkoutTemplates.Add(template);
        await dbContext.SaveChangesAsync();

        var result = await new WorkoutTemplateRepository(dbContext).GetByIdAsync(template.Id);

        result.Should().NotBeNull();
        result!.Name.Should().Be("Push");
        result.Exercises.Should().ContainSingle().Which.Sets.Should().ContainSingle().Which.Reps.Should().Be(8);
    }

    [Fact]
    public async Task GetByIdAsync_WithUnknownId_ReturnsNull()
    {
        var dbContext = DbContextFactory.CreateInMemory();

        var result = await new WorkoutTemplateRepository(dbContext).GetByIdAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    // --- AddAsync ---

    [Fact]
    public async Task AddAsync_PersistsTemplateForUserAndReturnsItsId()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var userId = Guid.NewGuid();
        var exerciseInfo = MakeExerciseInfo();
        dbContext.ExerciseInfos.Add(exerciseInfo);
        await dbContext.SaveChangesAsync();

        var id = await new WorkoutTemplateRepository(dbContext).AddAsync(
            userId, "Upper body", new List<WorkoutTemplateExerciseRequest> { ExerciseRequest(exerciseInfo.Id, 10) });

        var saved = dbContext.WorkoutTemplates.Should().ContainSingle().Subject;
        saved.Id.Should().Be(id);
        saved.Name.Should().Be("Upper body");
        saved.UserId.Should().Be(userId);
    }

    [Fact]
    public async Task AddAsync_NumbersExercisesAndSetsByTheirPositionInTheRequest()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var first = MakeExerciseInfo("Bench Press");
        var second = MakeExerciseInfo("Row");
        dbContext.ExerciseInfos.AddRange(first, second);
        await dbContext.SaveChangesAsync();

        var id = await new WorkoutTemplateRepository(dbContext).AddAsync(
            Guid.NewGuid(),
            "Pull",
            new List<WorkoutTemplateExerciseRequest>
            {
                ExerciseRequest(first.Id, 12, 10),
                ExerciseRequest(second.Id, 8)
            });

        var template = await new WorkoutTemplateRepository(dbContext).GetByIdAsync(id);
        var exercises = template!.Exercises.OrderBy(e => e.Order).ToList();
        exercises.Select(e => e.Order).Should().Equal(0, 1);
        exercises[0].ExerciseInfoId.Should().Be(first.Id);
        exercises[0].Sets.OrderBy(s => s.Order).Select(s => (s.Order, s.Reps)).Should().Equal((0, 12), (1, 10));
        exercises[1].Sets.Should().ContainSingle().Which.Reps.Should().Be(8);
    }

    [Fact]
    public async Task AddAsync_WithNoExercises_CreatesAnEmptyTemplate()
    {
        var dbContext = DbContextFactory.CreateInMemory();

        var id = await new WorkoutTemplateRepository(dbContext).AddAsync(
            Guid.NewGuid(), "Empty", new List<WorkoutTemplateExerciseRequest>());

        var template = await new WorkoutTemplateRepository(dbContext).GetByIdAsync(id);
        template!.Exercises.Should().BeEmpty();
    }

    // --- UpdateAsync ---

    [Fact]
    public async Task UpdateAsync_RenamesTemplateAndReplacesItsExercises()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var original = MakeExerciseInfo("Bench Press");
        var replacement = MakeExerciseInfo("Overhead Press");
        dbContext.ExerciseInfos.AddRange(original, replacement);
        await dbContext.SaveChangesAsync();

        var repo = new WorkoutTemplateRepository(dbContext);
        var id = await repo.AddAsync(Guid.NewGuid(), "Before", new List<WorkoutTemplateExerciseRequest>
        {
            ExerciseRequest(original.Id, 10, 10)
        });

        var success = await repo.UpdateAsync(id, "After", new List<WorkoutTemplateExerciseRequest>
        {
            ExerciseRequest(replacement.Id, 5)
        });

        success.Should().BeTrue();
        var template = await repo.GetByIdAsync(id);
        template!.Name.Should().Be("After");
        var exercise = template.Exercises.Should().ContainSingle().Subject;
        exercise.ExerciseInfoId.Should().Be(replacement.Id);
        exercise.Sets.Should().ContainSingle().Which.Reps.Should().Be(5);
    }

    [Fact]
    public async Task UpdateAsync_RemovesTheSetsOfTheExercisesItReplaces()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var exerciseInfo = MakeExerciseInfo();
        dbContext.ExerciseInfos.Add(exerciseInfo);
        await dbContext.SaveChangesAsync();

        var repo = new WorkoutTemplateRepository(dbContext);
        var id = await repo.AddAsync(Guid.NewGuid(), "Before", new List<WorkoutTemplateExerciseRequest>
        {
            ExerciseRequest(exerciseInfo.Id, 10, 10, 10)
        });

        await repo.UpdateAsync(id, "After", new List<WorkoutTemplateExerciseRequest>
        {
            ExerciseRequest(exerciseInfo.Id, 5)
        });

        // The three original sets should be gone rather than orphaned alongside the new one.
        dbContext.WorkoutTemplateExerciseSets.Should().ContainSingle();
        dbContext.WorkoutTemplateExercises.Should().ContainSingle();
    }

    [Fact]
    public async Task UpdateAsync_WithManyExistingExercises_DoesNotThrowWhileClearingThem()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var exerciseInfo = MakeExerciseInfo();
        dbContext.ExerciseInfos.Add(exerciseInfo);
        await dbContext.SaveChangesAsync();

        var repo = new WorkoutTemplateRepository(dbContext);
        var id = await repo.AddAsync(Guid.NewGuid(), "Before", Enumerable.Range(0, 5)
            .Select(_ => ExerciseRequest(exerciseInfo.Id, 10, 10))
            .ToList());

        var update = async () => await repo.UpdateAsync(id, "After", new List<WorkoutTemplateExerciseRequest>
        {
            ExerciseRequest(exerciseInfo.Id, 8)
        });

        await update.Should().NotThrowAsync();
        (await repo.GetByIdAsync(id))!.Exercises.Should().ContainSingle();
    }

    [Fact]
    public async Task UpdateAsync_WithUnknownTemplate_ReturnsFalse()
    {
        var dbContext = DbContextFactory.CreateInMemory();

        var success = await new WorkoutTemplateRepository(dbContext)
            .UpdateAsync(Guid.NewGuid(), "Nope", new List<WorkoutTemplateExerciseRequest>());

        success.Should().BeFalse();
    }

    // --- DeleteAsync ---

    [Fact]
    public async Task DeleteAsync_RemovesTheTemplateAndReturnsTrue()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var template = new Models.WorkoutTemplate { Id = Guid.NewGuid(), Name = "Doomed", UserId = Guid.NewGuid() };
        dbContext.WorkoutTemplates.Add(template);
        await dbContext.SaveChangesAsync();

        var success = await new WorkoutTemplateRepository(dbContext).DeleteAsync(template.Id);

        success.Should().BeTrue();
        dbContext.WorkoutTemplates.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteAsync_WithUnknownTemplate_ReturnsFalse()
    {
        var dbContext = DbContextFactory.CreateInMemory();

        var success = await new WorkoutTemplateRepository(dbContext).DeleteAsync(Guid.NewGuid());

        success.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_ClearsReferencingPlanDaysButKeepsTheDays()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var userId = Guid.NewGuid();
        var template = new Models.WorkoutTemplate { Id = Guid.NewGuid(), Name = "Referenced", UserId = userId };
        var plan = new Models.WorkoutPlan { Id = Guid.NewGuid(), UserId = userId, Name = "Plan", StartDate = DateTime.UtcNow };
        var day = new Models.WorkoutPlanDay
        {
            Id = Guid.NewGuid(),
            WorkoutPlanId = plan.Id,
            DayOfWeek = 1,
            WorkoutTemplateId = template.Id,
            Note = "Keep me"
        };
        dbContext.WorkoutTemplates.Add(template);
        dbContext.WorkoutPlans.Add(plan);
        dbContext.WorkoutPlanDays.Add(day);
        await dbContext.SaveChangesAsync();

        var success = await new WorkoutTemplateRepository(dbContext).DeleteAsync(template.Id);

        success.Should().BeTrue();
        var remaining = dbContext.WorkoutPlanDays.Should().ContainSingle().Subject;
        remaining.WorkoutTemplateId.Should().BeNull();
        remaining.Note.Should().Be("Keep me");
    }
}
