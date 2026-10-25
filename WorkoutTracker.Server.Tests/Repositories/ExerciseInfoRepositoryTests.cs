using FluentAssertions;
using Domain = WorkoutTracker.Server.DomainObjects;
using WorkoutTracker.Server.Enums;
using WorkoutTracker.Server.Models.Repositories;
using WorkoutTracker.Server.Tests.Helpers;
using Xunit;

namespace WorkoutTracker.Server.Tests.Repositories;

public class ExerciseInfoRepositoryTests
{
    private static Models.ExerciseInfo MakeModel(
        string name = "Bench Press",
        Guid? createdByUserId = null,
        string secondaryJson = "[]") => new()
        {
            Id = Guid.NewGuid(),
            Name = name,
            PrimaryMuscleGroup = MuscleGroup.Chest,
            SecondaryMuscleGroupsJson = secondaryJson,
            CreatedByUserId = createdByUserId
        };

    // --- GetByIdsAsync ---

    [Fact]
    public async Task GetByIdsAsync_ReturnsOnlyRequestedExerciseInfos()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var wanted = MakeModel("Wanted");
        var unwanted = MakeModel("Unwanted");
        dbContext.ExerciseInfos.AddRange(wanted, unwanted);
        await dbContext.SaveChangesAsync();

        var repo = new ExerciseInfoRepository(dbContext);
        var result = await repo.GetByIdsAsync(new List<Guid> { wanted.Id });

        result.Should().ContainSingle().Which.Name.Should().Be("Wanted");
    }

    [Fact]
    public async Task GetByIdsAsync_WithNoMatches_ReturnsEmptyList()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var repo = new ExerciseInfoRepository(dbContext);

        var result = await repo.GetByIdsAsync(new List<Guid> { Guid.NewGuid() });

        result.Should().BeEmpty();
    }

    // --- GetAllAsync ---

    [Fact]
    public async Task GetAllAsync_ReturnsAllExerciseInfos()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        dbContext.ExerciseInfos.AddRange(MakeModel("One"), MakeModel("Two", createdByUserId: Guid.NewGuid()));
        await dbContext.SaveChangesAsync();

        var repo = new ExerciseInfoRepository(dbContext);
        var result = await repo.GetAllAsync();

        result.Select(ei => ei.Name).Should().BeEquivalentTo("One", "Two");
    }

    // --- GetForUserAsync ---

    [Fact]
    public async Task GetForUserAsync_ReturnsGlobalAndUserOwnedButNotOtherUsers()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var userId = Guid.NewGuid();
        dbContext.ExerciseInfos.AddRange(
            MakeModel("Global"),
            MakeModel("Mine", createdByUserId: userId),
            MakeModel("Theirs", createdByUserId: Guid.NewGuid()));
        await dbContext.SaveChangesAsync();

        var repo = new ExerciseInfoRepository(dbContext);
        var result = await repo.GetForUserAsync(userId);

        result.Select(ei => ei.Name).Should().BeEquivalentTo("Global", "Mine");
    }

    // --- GetByIdAsync ---

    [Fact]
    public async Task GetByIdAsync_WithExistingId_ReturnsDomainObjectWithMuscleGroups()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var model = MakeModel(secondaryJson: "[3,4]"); // Biceps, Triceps
        dbContext.ExerciseInfos.Add(model);
        await dbContext.SaveChangesAsync();

        var repo = new ExerciseInfoRepository(dbContext);
        var result = await repo.GetByIdAsync(model.Id);

        result.Should().NotBeNull();
        result!.Name.Should().Be(model.Name);
        result.PrimaryMuscleGroup.Should().Be(MuscleGroup.Chest);
        result.SecondaryMuscleGroups.Should().BeEquivalentTo(new[] { MuscleGroup.Biceps, MuscleGroup.Triceps });
    }

    [Fact]
    public async Task GetByIdAsync_WithUnknownId_ReturnsNull()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var repo = new ExerciseInfoRepository(dbContext);

        var result = await repo.GetByIdAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    // --- Add ---

    [Fact]
    public async Task Add_PersistsExerciseInfoAndReturnsTrue()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var repo = new ExerciseInfoRepository(dbContext);
        var domain = new Domain.ExerciseInfo
        {
            Id = Guid.NewGuid(),
            Name = "Lat Pulldown",
            PrimaryMuscleGroup = MuscleGroup.Back,
            SecondaryMuscleGroups = new List<MuscleGroup> { MuscleGroup.Biceps }
        };

        var result = await repo.Add(domain);

        result.Should().BeTrue();
        var stored = dbContext.ExerciseInfos.Should().ContainSingle().Subject;
        stored.Name.Should().Be("Lat Pulldown");
        stored.SecondaryMuscleGroupsJson.Should().Be("[3]");
    }

    // --- Update ---

    [Fact]
    public async Task Update_WithExistingExerciseInfo_UpdatesFields()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var model = MakeModel("Old Name");
        dbContext.ExerciseInfos.Add(model);
        await dbContext.SaveChangesAsync();

        var repo = new ExerciseInfoRepository(dbContext);
        await repo.Update(new Domain.ExerciseInfo
        {
            Id = model.Id,
            Name = "New Name",
            PrimaryMuscleGroup = MuscleGroup.Legs,
            SecondaryMuscleGroups = new List<MuscleGroup> { MuscleGroup.Core }
        });

        var stored = dbContext.ExerciseInfos.Single(ei => ei.Id == model.Id);
        stored.Name.Should().Be("New Name");
        stored.PrimaryMuscleGroup.Should().Be(MuscleGroup.Legs);
        stored.SecondaryMuscleGroupsJson.Should().Be("[6]");
    }

    [Fact]
    public async Task Update_WithUnknownId_DoesNothing()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var repo = new ExerciseInfoRepository(dbContext);

        await repo.Update(new Domain.ExerciseInfo { Id = Guid.NewGuid(), Name = "Ghost" });

        dbContext.ExerciseInfos.Should().BeEmpty();
    }

    // --- Delete ---

    [Fact]
    public async Task Delete_WithExistingId_RemovesExerciseInfoAndReturnsTrue()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var model = MakeModel();
        dbContext.ExerciseInfos.Add(model);
        await dbContext.SaveChangesAsync();

        var repo = new ExerciseInfoRepository(dbContext);
        var result = await repo.Delete(model.Id);

        result.Should().BeTrue();
        dbContext.ExerciseInfos.Should().BeEmpty();
    }

    [Fact]
    public async Task Delete_WithUnknownId_ReturnsFalse()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var repo = new ExerciseInfoRepository(dbContext);

        var result = await repo.Delete(Guid.NewGuid());

        result.Should().BeFalse();
    }
}
