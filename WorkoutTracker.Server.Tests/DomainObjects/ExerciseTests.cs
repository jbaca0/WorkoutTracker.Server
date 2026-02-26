using FluentAssertions;
using WorkoutTracker.Server.DomainObjects;
using WorkoutTracker.Server.Enums;
using Xunit;

namespace WorkoutTracker.Server.Tests.DomainObjects;

public class ExerciseTests
{
    private static Models.ExerciseInfo MakeExerciseInfoModel(Guid? id = null) => new()
    {
        Id = id ?? Guid.NewGuid(),
        Name = "Bench Press",
        PrimaryMuscleGroup = MuscleGroup.Chest,
        SecondaryMuscleGroupsJson = "[]"
    };

    [Fact]
    public void FromModel_MapsAllProperties()
    {
        var exerciseInfoId = Guid.NewGuid();
        var model = new Models.Exercise
        {
            Id = Guid.NewGuid(),
            WorkoutId = Guid.NewGuid(),
            ExerciseInfoId = exerciseInfoId,
            ExerciseInfo = MakeExerciseInfoModel(exerciseInfoId),
            Sets = new List<Models.Set>
            {
                new() { Id = Guid.NewGuid(), ExerciseId = Guid.NewGuid(), Reps = 10, Weight = 135m, Completed = true }
            }
        };

        var result = Exercise.FromModel(model);

        result.Id.Should().Be(model.Id);
        result.WorkoutId.Should().Be(model.WorkoutId);
        result.ExerciseInfoId.Should().Be(exerciseInfoId);
        result.ExerciseInfo.Should().NotBeNull();
        result.ExerciseInfo.Name.Should().Be("Bench Press");
        result.Sets.Should().HaveCount(1);
        result.Sets[0].Reps.Should().Be(10);
    }

    [Fact]
    public void FromModel_WithNullSets_ReturnsEmptyList()
    {
        var exerciseInfoId = Guid.NewGuid();
        var model = new Models.Exercise
        {
            Id = Guid.NewGuid(),
            WorkoutId = Guid.NewGuid(),
            ExerciseInfoId = exerciseInfoId,
            ExerciseInfo = MakeExerciseInfoModel(exerciseInfoId),
            Sets = null!
        };

        var result = Exercise.FromModel(model);

        result.Sets.Should().BeEmpty();
    }

    [Fact]
    public void ToModel_MapsAllProperties()
    {
        var exerciseInfoId = Guid.NewGuid();
        var exercise = new Exercise
        {
            Id = Guid.NewGuid(),
            WorkoutId = Guid.NewGuid(),
            ExerciseInfoId = exerciseInfoId,
            ExerciseInfo = new ExerciseInfo { Id = exerciseInfoId, Name = "Squat", PrimaryMuscleGroup = MuscleGroup.Legs },
            Sets = new List<Set>
            {
                new() { Id = Guid.NewGuid(), ExerciseId = Guid.NewGuid(), Reps = 5, Weight = 225m, Completed = true }
            }
        };

        var result = exercise.ToModel();

        result.Id.Should().Be(exercise.Id);
        result.WorkoutId.Should().Be(exercise.WorkoutId);
        result.ExerciseInfoId.Should().Be(exerciseInfoId);
        result.Sets.Should().HaveCount(1);
        result.Sets.First().Reps.Should().Be(5);
    }

    [Fact]
    public void FromData_SetsExerciseInfoId()
    {
        var exerciseInfoId = Guid.NewGuid();
        var data = new DataTransferObjects.Common.Exercise
        {
            exerciseInfoId = exerciseInfoId,
            sets = new List<DataTransferObjects.Common.Set>
            {
                new() { reps = 8, weight = 100m, completed = false }
            }
        };

        var result = Exercise.FromData(data);

        result.ExerciseInfoId.Should().Be(exerciseInfoId);
        result.ExerciseInfo.Id.Should().Be(exerciseInfoId);
        result.Sets.Should().HaveCount(1);
        result.Sets[0].Reps.Should().Be(8);
    }

    [Fact]
    public void FromData_WithNullSets_ReturnsEmptyList()
    {
        var data = new DataTransferObjects.Common.Exercise
        {
            exerciseInfoId = Guid.NewGuid(),
            sets = null!
        };

        var result = Exercise.FromData(data);

        result.Sets.Should().BeEmpty();
    }

    [Fact]
    public void ToData_MapsAllProperties()
    {
        var exerciseInfoId = Guid.NewGuid();
        var exercise = new Exercise
        {
            Id = Guid.NewGuid(),
            WorkoutId = Guid.NewGuid(),
            ExerciseInfoId = exerciseInfoId,
            ExerciseInfo = new ExerciseInfo { Id = exerciseInfoId, Name = "Deadlift", PrimaryMuscleGroup = MuscleGroup.Back },
            Sets = new List<Set>
            {
                new() { Id = Guid.NewGuid(), ExerciseId = Guid.NewGuid(), Reps = 3, Weight = 405m, Completed = true }
            }
        };

        var result = exercise.ToData();

        result.exerciseInfoId.Should().Be(exerciseInfoId);
        result.exerciseInfo!.name.Should().Be("Deadlift");
        result.sets.Should().HaveCount(1);
        result.sets[0].reps.Should().Be(3);
    }
}
