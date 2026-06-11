using FluentAssertions;
using WorkoutTracker.Server.DomainObjects;
using WorkoutTracker.Server.Enums;
using WorkoutTracker.Server.Models.Repositories;
using WorkoutTracker.Server.Tests.Helpers;
using Xunit;

namespace WorkoutTracker.Server.Tests.Repositories;

public class WorkoutRepositoryTests
{
    // --- GetByIdAsync ---

    [Fact]
    public async Task GetByIdAsync_WithExistingWorkout_ReturnsWorkout()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var workout = new Models.Workout { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), AddDateTime = DateTime.UtcNow };
        dbContext.Workouts.Add(workout);
        await dbContext.SaveChangesAsync();

        var repo = new WorkoutRepository(dbContext);
        var result = await repo.GetByIdAsync(workout.Id);

        result.Should().NotBeNull();
        result!.Id.Should().Be(workout.Id);
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistentId_ReturnsNull()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var repo = new WorkoutRepository(dbContext);

        var result = await repo.GetByIdAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    // --- AddExercises ---

    [Fact]
    public async Task AddExercises_WithValidWorkoutId_AddsExercisesAndReturnsTrue()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var workoutId = Guid.NewGuid();
        var exerciseInfoId = Guid.NewGuid();

        dbContext.Workouts.Add(new Models.Workout { Id = workoutId, UserId = Guid.NewGuid(), AddDateTime = DateTime.UtcNow });
        dbContext.ExerciseInfos.Add(new Models.ExerciseInfo { Id = exerciseInfoId, Name = "Bench Press", SecondaryMuscleGroupsJson = "[]" });
        await dbContext.SaveChangesAsync();

        var repo = new WorkoutRepository(dbContext);
        var result = await repo.AddExercises(workoutId, new[] { exerciseInfoId });

        result.Should().BeTrue();
        dbContext.Exercises.Should().HaveCount(1);
        dbContext.Exercises.First().WorkoutId.Should().Be(workoutId);
        dbContext.Exercises.First().ExerciseInfoId.Should().Be(exerciseInfoId);
    }

    [Fact]
    public async Task AddExercises_WithInvalidWorkoutId_ReturnsFalse()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var repo = new WorkoutRepository(dbContext);

        var result = await repo.AddExercises(Guid.NewGuid(), new[] { Guid.NewGuid() });

        result.Should().BeFalse();
    }

    // --- SaveAsync (new workout) ---

    [Fact]
    public async Task SaveAsync_WithNewWorkout_InsertsAndReturnsTrue()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var repo = new WorkoutRepository(dbContext);

        var workout = new Workout
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            AddDateTime = DateTime.UtcNow,
            Exercises = new List<Exercise>()
        };

        var result = await repo.SaveAsync(workout);

        result.Should().BeTrue();
        dbContext.Workouts.Should().HaveCount(1);
        dbContext.Workouts.First().Id.Should().Be(workout.Id);
    }

    // --- SaveAsync (existing workout - updates exercises) ---

    [Fact]
    public async Task SaveAsync_WithExistingWorkout_ReplacesExercisesAndReturnsTrue()
    {
        var dbContext = DbContextFactory.CreateInMemory();

        var workoutId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var exerciseInfoId1 = Guid.NewGuid();
        var exerciseInfoId2 = Guid.NewGuid();

        dbContext.Workouts.Add(new Models.Workout { Id = workoutId, UserId = userId, AddDateTime = DateTime.UtcNow });
        dbContext.ExerciseInfos.Add(new Models.ExerciseInfo { Id = exerciseInfoId1, Name = "Old Exercise", SecondaryMuscleGroupsJson = "[]" });
        dbContext.ExerciseInfos.Add(new Models.ExerciseInfo { Id = exerciseInfoId2, Name = "New Exercise", SecondaryMuscleGroupsJson = "[]" });
        var oldExercise = new Models.Exercise { Id = Guid.NewGuid(), WorkoutId = workoutId, ExerciseInfoId = exerciseInfoId1 };
        dbContext.Exercises.Add(oldExercise);
        await dbContext.SaveChangesAsync();

        var repo = new WorkoutRepository(dbContext);
        var workout = new Workout
        {
            Id = workoutId,
            UserId = userId,
            AddDateTime = DateTime.UtcNow,
            Exercises = new List<Exercise>
            {
                new()
                {
                    ExerciseInfoId = exerciseInfoId2,
                    ExerciseInfo = new ExerciseInfo { Id = exerciseInfoId2, Name = "New Exercise", PrimaryMuscleGroup = MuscleGroup.Back },
                    Sets = new List<Set>()
                }
            }
        };

        var result = await repo.SaveAsync(workout);

        result.Should().BeTrue();
        dbContext.Exercises.Should().HaveCount(1);
        dbContext.Exercises.First().ExerciseInfoId.Should().Be(exerciseInfoId2);
    }

    // --- GetWorkoutsWithSummaryAsync ---

    [Fact]
    public async Task GetWorkoutsWithSummaryAsync_ReturnsOnlyUserWorkouts()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();

        dbContext.Workouts.Add(new Models.Workout { Id = Guid.NewGuid(), UserId = userId, AddDateTime = DateTime.UtcNow });
        dbContext.Workouts.Add(new Models.Workout { Id = Guid.NewGuid(), UserId = otherUserId, AddDateTime = DateTime.UtcNow });
        await dbContext.SaveChangesAsync();

        var repo = new WorkoutRepository(dbContext);
        var result = await repo.GetWorkoutsWithSummaryAsync(userId);

        result.Should().HaveCount(1);
        result[0].UserId.Should().Be(userId);
    }

    [Fact]
    public async Task GetWorkoutsWithSummaryAsync_ReturnsWorkoutsOrderedByDateDescending()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var userId = Guid.NewGuid();

        var older = new Models.Workout { Id = Guid.NewGuid(), UserId = userId, AddDateTime = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) };
        var newer = new Models.Workout { Id = Guid.NewGuid(), UserId = userId, AddDateTime = new DateTime(2024, 6, 1, 0, 0, 0, DateTimeKind.Utc) };
        dbContext.Workouts.AddRange(older, newer);
        await dbContext.SaveChangesAsync();

        var repo = new WorkoutRepository(dbContext);
        var result = await repo.GetWorkoutsWithSummaryAsync(userId);

        result.Should().HaveCount(2);
        result[0].AddDateTime.Should().BeAfter(result[1].AddDateTime);
    }

    // --- GetExercisesAsync ---

    [Fact]
    public async Task GetExercisesAsync_ReturnsOnlyExercisesForTheGivenWorkout()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var workoutId = Guid.NewGuid();
        var otherWorkoutId = Guid.NewGuid();
        var benchInfo = new Models.ExerciseInfo { Id = Guid.NewGuid(), Name = "Bench Press", SecondaryMuscleGroupsJson = "[]" };
        var squatInfo = new Models.ExerciseInfo { Id = Guid.NewGuid(), Name = "Squat", SecondaryMuscleGroupsJson = "[]" };

        dbContext.Workouts.AddRange(
            new Models.Workout { Id = workoutId, UserId = Guid.NewGuid() },
            new Models.Workout { Id = otherWorkoutId, UserId = Guid.NewGuid() });
        dbContext.ExerciseInfos.AddRange(benchInfo, squatInfo);
        dbContext.Exercises.AddRange(
            new Models.Exercise { Id = Guid.NewGuid(), WorkoutId = workoutId, ExerciseInfoId = benchInfo.Id },
            new Models.Exercise { Id = Guid.NewGuid(), WorkoutId = otherWorkoutId, ExerciseInfoId = squatInfo.Id });
        await dbContext.SaveChangesAsync();

        var repo = new WorkoutRepository(dbContext);
        var result = await repo.GetExercisesAsync(workoutId);

        result.Should().ContainSingle();
        result[0].ExerciseInfo.Name.Should().Be("Bench Press");
    }

    [Fact]
    public async Task GetExercisesAsync_WithUnknownWorkout_ReturnsEmptyList()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var repo = new WorkoutRepository(dbContext);

        var result = await repo.GetExercisesAsync(Guid.NewGuid());

        result.Should().BeEmpty();
    }

    // --- GetWorkoutWithDetailsAsync ---

    [Fact]
    public async Task GetWorkoutWithDetailsAsync_ReturnsWorkoutWithExercisesAndOrderedSets()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var workoutId = Guid.NewGuid();
        var exerciseInfo = new Models.ExerciseInfo { Id = Guid.NewGuid(), Name = "Deadlift", SecondaryMuscleGroupsJson = "[]" };
        var exercise = new Models.Exercise { Id = Guid.NewGuid(), WorkoutId = workoutId, ExerciseInfoId = exerciseInfo.Id };
        exercise.Sets.Add(new Models.Set { Id = Guid.NewGuid(), ExerciseId = exercise.Id, Reps = 3, Weight = 225m, Order = 2 });
        exercise.Sets.Add(new Models.Set { Id = Guid.NewGuid(), ExerciseId = exercise.Id, Reps = 5, Weight = 185m, Order = 1 });

        dbContext.Workouts.Add(new Models.Workout { Id = workoutId, UserId = Guid.NewGuid(), AddDateTime = DateTime.UtcNow });
        dbContext.ExerciseInfos.Add(exerciseInfo);
        dbContext.Exercises.Add(exercise);
        await dbContext.SaveChangesAsync();

        var repo = new WorkoutRepository(dbContext);
        var result = await repo.GetWorkoutWithDetailsAsync(workoutId);

        result.Should().NotBeNull();
        result!.Id.Should().Be(workoutId);
        var domainExercise = result.Exercises.Should().ContainSingle().Subject;
        domainExercise.ExerciseInfo.Name.Should().Be("Deadlift");
        domainExercise.Sets.Should().HaveCount(2);
        domainExercise.Sets.Select(s => s.Order).Should().ContainInOrder(1, 2);
    }

    [Fact]
    public async Task GetWorkoutWithDetailsAsync_WithUnknownWorkout_ReturnsNull()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var repo = new WorkoutRepository(dbContext);

        var result = await repo.GetWorkoutWithDetailsAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    // --- DeleteAsync ---

    [Fact]
    public async Task DeleteAsync_WithExistingWorkout_RemovesWorkoutWithExercisesAndSets()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var workoutId = Guid.NewGuid();
        var exerciseInfo = new Models.ExerciseInfo { Id = Guid.NewGuid(), Name = "Row", SecondaryMuscleGroupsJson = "[]" };
        var exercise = new Models.Exercise { Id = Guid.NewGuid(), WorkoutId = workoutId, ExerciseInfoId = exerciseInfo.Id };
        exercise.Sets.Add(new Models.Set { Id = Guid.NewGuid(), ExerciseId = exercise.Id, Reps = 10, Weight = 95m, Order = 1 });

        dbContext.Workouts.Add(new Models.Workout { Id = workoutId, UserId = Guid.NewGuid() });
        dbContext.ExerciseInfos.Add(exerciseInfo);
        dbContext.Exercises.Add(exercise);
        await dbContext.SaveChangesAsync();

        var repo = new WorkoutRepository(dbContext);
        var result = await repo.DeleteAsync(workoutId);

        result.Should().BeTrue();
        dbContext.Workouts.Should().BeEmpty();
        dbContext.Exercises.Should().BeEmpty();
        dbContext.Sets.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteAsync_WithUnknownWorkout_ReturnsFalse()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var repo = new WorkoutRepository(dbContext);

        var result = await repo.DeleteAsync(Guid.NewGuid());

        result.Should().BeFalse();
    }
}
