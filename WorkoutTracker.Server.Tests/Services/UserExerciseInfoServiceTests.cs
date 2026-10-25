using FluentAssertions;
using WorkoutTracker.Server.Models.Repositories;
using WorkoutTracker.Server.Services;
using WorkoutTracker.Server.Tests.Helpers;
using Xunit;

namespace WorkoutTracker.Server.Tests.Services;

public class UserExerciseInfoServiceTests
{
    [Fact]
    public async Task GetByIdsAsync_WithNullUserId_ReturnsEmptyList()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var exerciseRepository = new ExerciseRepository(dbContext);
        var exerciseGoalRepository = new ExerciseGoalRepository(dbContext);
        var workoutPlanRepository = new WorkoutPlanRepository(dbContext);
        var service = new UserExerciseInfoService(exerciseRepository, exerciseGoalRepository, workoutPlanRepository);

        var result = await service.GetByIdsAsync(null, new List<Guid> { Guid.NewGuid() });

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByIdsAsync_WithValidUserIdAndNoMatchingExercises_ReturnsEmptyList()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var exerciseRepository = new ExerciseRepository(dbContext);
        var exerciseGoalRepository = new ExerciseGoalRepository(dbContext);
        var workoutPlanRepository = new WorkoutPlanRepository(dbContext);
        var service = new UserExerciseInfoService(exerciseRepository, exerciseGoalRepository, workoutPlanRepository);
        var userId = Guid.NewGuid();
        var exerciseInfoIds = new List<Guid> { Guid.NewGuid() };

        var result = await service.GetByIdsAsync(userId, exerciseInfoIds);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByIdsAsync_WithMatchingExercise_ReturnsUserExerciseInfoList()
    {
        var dbContext = DbContextFactory.CreateInMemory();

        var userId = Guid.NewGuid();
        var exerciseInfoId = Guid.NewGuid();

        var exerciseInfo = new Models.ExerciseInfo
        {
            Id = exerciseInfoId,
            Name = "Bench Press",
            SecondaryMuscleGroupsJson = "[]"
        };
        dbContext.ExerciseInfos.Add(exerciseInfo);

        var workout = new Models.Workout
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            AddDateTime = DateTime.UtcNow
        };
        dbContext.Workouts.Add(workout);

        var exercise = new Models.Exercise
        {
            Id = Guid.NewGuid(),
            WorkoutId = workout.Id,
            ExerciseInfoId = exerciseInfoId,
            ExerciseInfo = exerciseInfo
        };
        exercise.Sets.Add(new Models.Set
        {
            Id = Guid.NewGuid(),
            ExerciseId = exercise.Id,
            Reps = 10,
            Weight = 135m,
            Completed = true
        });
        dbContext.Exercises.Add(exercise);
        await dbContext.SaveChangesAsync();

        var exerciseRepository = new ExerciseRepository(dbContext);
        var exerciseGoalRepository = new ExerciseGoalRepository(dbContext);
        var workoutPlanRepository = new WorkoutPlanRepository(dbContext);
        var service = new UserExerciseInfoService(exerciseRepository, exerciseGoalRepository, workoutPlanRepository);

        var result = await service.GetByIdsAsync(userId, new List<Guid> { exerciseInfoId });

        result.Should().HaveCount(1);
        result[0].ExerciseInfo.Id.Should().Be(exerciseInfoId);
        result[0].LatestExercise.Should().NotBeNull();
        result[0].LatestExercise!.Sets.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetByIdsAsync_ExerciseBelongingToDifferentUser_DoesNotLeakOtherUsersData()
    {
        var dbContext = DbContextFactory.CreateInMemory();

        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var exerciseInfoId = Guid.NewGuid();

        var exerciseInfo = new Models.ExerciseInfo
        {
            Id = exerciseInfoId,
            Name = "Squat",
            SecondaryMuscleGroupsJson = "[]"
        };
        dbContext.ExerciseInfos.Add(exerciseInfo);

        var workout = new Models.Workout
        {
            Id = Guid.NewGuid(),
            UserId = otherUserId, // different user
            AddDateTime = DateTime.UtcNow
        };
        dbContext.Workouts.Add(workout);

        var exercise = new Models.Exercise
        {
            Id = Guid.NewGuid(),
            WorkoutId = workout.Id,
            ExerciseInfoId = exerciseInfoId,
            ExerciseInfo = exerciseInfo
        };
        dbContext.Exercises.Add(exercise);
        await dbContext.SaveChangesAsync();

        var exerciseRepository = new ExerciseRepository(dbContext);
        var exerciseGoalRepository = new ExerciseGoalRepository(dbContext);
        var workoutPlanRepository = new WorkoutPlanRepository(dbContext);
        var service = new UserExerciseInfoService(exerciseRepository, exerciseGoalRepository, workoutPlanRepository);

        var result = await service.GetByIdsAsync(userId, new List<Guid> { exerciseInfoId });

        // ExerciseInfo is global data, so one entry per requested id is returned,
        // but the other user's exercise data must not leak to the requesting user.
        result.Should().HaveCount(1);
        result[0].ExerciseInfo.Id.Should().Be(exerciseInfoId);
        result[0].LatestExercise.Should().BeNull();
    }
}
