using FluentAssertions;
using WorkoutTracker.Server.Models.Repositories;
using WorkoutTracker.Server.Tests.Helpers;
using Xunit;

namespace WorkoutTracker.Server.Tests.Repositories;

public class ExerciseRepositoryTests
{
    [Fact]
    public async Task GetLatestExercisesForUserAsync_ReturnsOnlyExercisesForUser()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var exerciseInfoId = Guid.NewGuid();

        var exerciseInfo = new Models.ExerciseInfo { Id = exerciseInfoId, Name = "Squat", SecondaryMuscleGroupsJson = "[]" };
        dbContext.ExerciseInfos.Add(exerciseInfo);

        var userWorkout = new Models.Workout { Id = Guid.NewGuid(), UserId = userId, AddDateTime = DateTime.UtcNow };
        var otherWorkout = new Models.Workout { Id = Guid.NewGuid(), UserId = otherUserId, AddDateTime = DateTime.UtcNow };
        dbContext.Workouts.AddRange(userWorkout, otherWorkout);

        dbContext.Exercises.Add(new Models.Exercise { Id = Guid.NewGuid(), WorkoutId = userWorkout.Id, ExerciseInfoId = exerciseInfoId, ExerciseInfo = exerciseInfo });
        dbContext.Exercises.Add(new Models.Exercise { Id = Guid.NewGuid(), WorkoutId = otherWorkout.Id, ExerciseInfoId = exerciseInfoId, ExerciseInfo = exerciseInfo });
        await dbContext.SaveChangesAsync();

        var repo = new ExerciseRepository(dbContext);
        var result = await repo.GetLatestExercisesForUserAsync(userId, new List<Guid> { exerciseInfoId });

        result.Should().HaveCount(1);
        result[0].ExerciseInfo.Id.Should().Be(exerciseInfoId);
    }

    [Fact]
    public async Task GetLatestExercisesForUserAsync_ReturnsOnlyLatestExercisePerExerciseInfo()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var userId = Guid.NewGuid();
        var exerciseInfoId = Guid.NewGuid();

        var exerciseInfo = new Models.ExerciseInfo { Id = exerciseInfoId, Name = "Deadlift", SecondaryMuscleGroupsJson = "[]" };
        dbContext.ExerciseInfos.Add(exerciseInfo);

        var olderWorkout = new Models.Workout { Id = Guid.NewGuid(), UserId = userId, AddDateTime = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) };
        var newerWorkout = new Models.Workout { Id = Guid.NewGuid(), UserId = userId, AddDateTime = new DateTime(2024, 6, 1, 0, 0, 0, DateTimeKind.Utc) };
        dbContext.Workouts.AddRange(olderWorkout, newerWorkout);

        var olderExercise = new Models.Exercise { Id = Guid.NewGuid(), WorkoutId = olderWorkout.Id, ExerciseInfoId = exerciseInfoId, ExerciseInfo = exerciseInfo };
        var newerExercise = new Models.Exercise { Id = Guid.NewGuid(), WorkoutId = newerWorkout.Id, ExerciseInfoId = exerciseInfoId, ExerciseInfo = exerciseInfo };
        olderExercise.Sets.Add(new Models.Set { Id = Guid.NewGuid(), ExerciseId = olderExercise.Id, Reps = 5, Weight = 300m });
        newerExercise.Sets.Add(new Models.Set { Id = Guid.NewGuid(), ExerciseId = newerExercise.Id, Reps = 5, Weight = 350m });
        dbContext.Exercises.AddRange(olderExercise, newerExercise);
        await dbContext.SaveChangesAsync();

        var repo = new ExerciseRepository(dbContext);
        var result = await repo.GetLatestExercisesForUserAsync(userId, new List<Guid> { exerciseInfoId });

        result.Should().HaveCount(1);
        result[0].Sets.Should().HaveCount(1);
        result[0].Sets[0].Weight.Should().Be(350m);
    }

    [Fact]
    public async Task GetLatestExercisesForUserAsync_WithNoMatchingExerciseInfoIds_ReturnsEmpty()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var userId = Guid.NewGuid();
        var exerciseInfoId = Guid.NewGuid();

        var exerciseInfo = new Models.ExerciseInfo { Id = exerciseInfoId, Name = "Row", SecondaryMuscleGroupsJson = "[]" };
        dbContext.ExerciseInfos.Add(exerciseInfo);

        var workout = new Models.Workout { Id = Guid.NewGuid(), UserId = userId, AddDateTime = DateTime.UtcNow };
        dbContext.Workouts.Add(workout);

        dbContext.Exercises.Add(new Models.Exercise { Id = Guid.NewGuid(), WorkoutId = workout.Id, ExerciseInfoId = exerciseInfoId, ExerciseInfo = exerciseInfo });
        await dbContext.SaveChangesAsync();

        var repo = new ExerciseRepository(dbContext);
        // Query with a different exerciseInfoId
        var result = await repo.GetLatestExercisesForUserAsync(userId, new List<Guid> { Guid.NewGuid() });

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetLatestExercisesForUserAsync_ReturnsLatestForMultipleExerciseInfoIds()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var userId = Guid.NewGuid();
        var exerciseInfoId1 = Guid.NewGuid();
        var exerciseInfoId2 = Guid.NewGuid();

        var exerciseInfo1 = new Models.ExerciseInfo { Id = exerciseInfoId1, Name = "Squat", SecondaryMuscleGroupsJson = "[]" };
        var exerciseInfo2 = new Models.ExerciseInfo { Id = exerciseInfoId2, Name = "Bench Press", SecondaryMuscleGroupsJson = "[]" };
        dbContext.ExerciseInfos.AddRange(exerciseInfo1, exerciseInfo2);

        var workout = new Models.Workout { Id = Guid.NewGuid(), UserId = userId, AddDateTime = DateTime.UtcNow };
        dbContext.Workouts.Add(workout);

        dbContext.Exercises.Add(new Models.Exercise { Id = Guid.NewGuid(), WorkoutId = workout.Id, ExerciseInfoId = exerciseInfoId1, ExerciseInfo = exerciseInfo1 });
        dbContext.Exercises.Add(new Models.Exercise { Id = Guid.NewGuid(), WorkoutId = workout.Id, ExerciseInfoId = exerciseInfoId2, ExerciseInfo = exerciseInfo2 });
        await dbContext.SaveChangesAsync();

        var repo = new ExerciseRepository(dbContext);
        var result = await repo.GetLatestExercisesForUserAsync(userId, new List<Guid> { exerciseInfoId1, exerciseInfoId2 });

        result.Should().HaveCount(2);
        result.Select(e => e.ExerciseInfo.Id).Should().BeEquivalentTo(new[] { exerciseInfoId1, exerciseInfoId2 });
    }
}
