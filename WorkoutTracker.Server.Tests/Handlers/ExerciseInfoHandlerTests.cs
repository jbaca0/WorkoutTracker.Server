using FluentAssertions;
using Microsoft.AspNetCore.Http.HttpResults;
using WorkoutTracker.Server.DataTransferObjects.Requests;
using WorkoutTracker.Server.DataTransferObjects.Responses;
using WorkoutTracker.Server.Enums;
using WorkoutTracker.Server.Handlers;
using WorkoutTracker.Server.Models.Repositories;
using WorkoutTracker.Server.Tests.Helpers;
using Xunit;

namespace WorkoutTracker.Server.Tests.Handlers;

public class ExerciseInfoHandlerTests
{
    // --- AddExerciseInfo ---

    [Fact]
    public async Task AddExerciseInfo_WithAuthenticatedUser_PersistsWithCreatedByUserId()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var userId = Guid.NewGuid();
        var request = new AddExerciseInfoRequest
        {
            name = "Cable Row",
            primaryMuscleGroup = MuscleGroup.Back,
            secondaryMuscleGroups = [MuscleGroup.Biceps]
        };

        var result = await ExerciseInfoHandler.AddExerciseInfo(
            ClaimsPrincipalBuilder.WithUserId(userId),
            new ExerciseInfoRepository(dbContext),
            request);

        result.Should().BeOfType<Ok>();
        var stored = dbContext.ExerciseInfos.Should().ContainSingle().Subject;
        stored.Name.Should().Be("Cable Row");
        stored.PrimaryMuscleGroup.Should().Be(MuscleGroup.Back);
        stored.CreatedByUserId.Should().Be(userId);
    }

    [Fact]
    public async Task AddExerciseInfo_WithoutUserIdClaim_PersistsAsGlobalExercise()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var request = new AddExerciseInfoRequest { name = "Push Up" };

        var result = await ExerciseInfoHandler.AddExerciseInfo(
            ClaimsPrincipalBuilder.WithNoUserId(),
            new ExerciseInfoRepository(dbContext),
            request);

        result.Should().BeOfType<Ok>();
        var stored = dbContext.ExerciseInfos.Should().ContainSingle().Subject;
        stored.CreatedByUserId.Should().BeNull();
    }

    // --- GetExerciseInfos ---

    [Fact]
    public async Task GetExerciseInfos_WithUserId_ReturnsGlobalAndOwnExercisesOnly()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var userId = Guid.NewGuid();
        dbContext.ExerciseInfos.AddRange(
            new Models.ExerciseInfo { Id = Guid.NewGuid(), Name = "Global", CreatedByUserId = null, SecondaryMuscleGroupsJson = "[]" },
            new Models.ExerciseInfo { Id = Guid.NewGuid(), Name = "Mine", CreatedByUserId = userId, SecondaryMuscleGroupsJson = "[]" },
            new Models.ExerciseInfo { Id = Guid.NewGuid(), Name = "Someone Elses", CreatedByUserId = Guid.NewGuid(), SecondaryMuscleGroupsJson = "[]" });
        await dbContext.SaveChangesAsync();

        var result = await ExerciseInfoHandler.GetExerciseInfos(
            ClaimsPrincipalBuilder.WithUserId(userId),
            new ExerciseInfoRepository(dbContext));

        var ok = result.Should().BeOfType<Ok<GetExerciseInfosResponse>>().Subject;
        ok.Value!.exerciseInfos.Select(ei => ei.name).Should().BeEquivalentTo("Global", "Mine");
    }

    [Fact]
    public async Task GetExerciseInfos_WithoutUserIdClaim_ReturnsAllExercises()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        dbContext.ExerciseInfos.AddRange(
            new Models.ExerciseInfo { Id = Guid.NewGuid(), Name = "Global", CreatedByUserId = null, SecondaryMuscleGroupsJson = "[]" },
            new Models.ExerciseInfo { Id = Guid.NewGuid(), Name = "User Owned", CreatedByUserId = Guid.NewGuid(), SecondaryMuscleGroupsJson = "[]" });
        await dbContext.SaveChangesAsync();

        var result = await ExerciseInfoHandler.GetExerciseInfos(
            ClaimsPrincipalBuilder.WithNoUserId(),
            new ExerciseInfoRepository(dbContext));

        var ok = result.Should().BeOfType<Ok<GetExerciseInfosResponse>>().Subject;
        ok.Value!.exerciseInfos.Should().HaveCount(2);
    }
}
