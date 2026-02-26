using FluentAssertions;
using WorkoutTracker.Server.DomainObjects;
using WorkoutTracker.Server.Enums;
using Xunit;

namespace WorkoutTracker.Server.Tests.DomainObjects;

public class WorkoutTests
{
    private static Models.ExerciseInfo MakeExerciseInfoModel() => new()
    {
        Id = Guid.NewGuid(),
        Name = "Bench Press",
        PrimaryMuscleGroup = MuscleGroup.Chest,
        SecondaryMuscleGroupsJson = "[]"
    };

    [Fact]
    public void FromModel_MapsAllProperties()
    {
        var exerciseInfoModel = MakeExerciseInfoModel();
        var model = new Models.Workout
        {
            Id = Guid.NewGuid(),
            AddDateTime = new DateTime(2024, 1, 15, 10, 0, 0, DateTimeKind.Utc),
            UserId = Guid.NewGuid(),
            Exercises = new List<Models.Exercise>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    WorkoutId = Guid.NewGuid(),
                    ExerciseInfoId = exerciseInfoModel.Id,
                    ExerciseInfo = exerciseInfoModel,
                    Sets = new List<Models.Set>()
                }
            }
        };

        var result = Workout.FromModel(model);

        result.Id.Should().Be(model.Id);
        result.AddDateTime.Should().Be(model.AddDateTime);
        result.UserId.Should().Be(model.UserId);
        result.Exercises.Should().HaveCount(1);
    }

    [Fact]
    public void FromModel_WithNullExercises_ReturnsEmptyList()
    {
        var model = new Models.Workout
        {
            Id = Guid.NewGuid(),
            AddDateTime = DateTime.UtcNow,
            UserId = Guid.NewGuid(),
            Exercises = null!
        };

        var result = Workout.FromModel(model);

        result.Exercises.Should().BeEmpty();
    }

    [Fact]
    public void ToModel_MapsAllProperties()
    {
        var exerciseInfoId = Guid.NewGuid();
        var workout = new Workout
        {
            Id = Guid.NewGuid(),
            AddDateTime = new DateTime(2024, 3, 1, 8, 0, 0, DateTimeKind.Utc),
            UserId = Guid.NewGuid(),
            Exercises = new List<Exercise>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    WorkoutId = Guid.NewGuid(),
                    ExerciseInfoId = exerciseInfoId,
                    ExerciseInfo = new ExerciseInfo { Id = exerciseInfoId, Name = "Row", PrimaryMuscleGroup = MuscleGroup.Back },
                    Sets = new List<Set>()
                }
            }
        };

        var result = workout.ToModel();

        result.Id.Should().Be(workout.Id);
        result.AddDateTime.Should().Be(workout.AddDateTime);
        result.UserId.Should().Be(workout.UserId);
        result.Exercises.Should().HaveCount(1);
    }

    [Fact]
    public void FromData_MapsIdAddDateTimeAndExercises()
    {
        var exerciseInfoId = Guid.NewGuid();
        var data = new DataTransferObjects.Common.Workout
        {
            id = Guid.NewGuid(),
            addDateTime = new DateTime(2024, 6, 10, 12, 0, 0, DateTimeKind.Utc),
            exercises = new List<DataTransferObjects.Common.Exercise>
            {
                new() { exerciseInfoId = exerciseInfoId, sets = new List<DataTransferObjects.Common.Set>() }
            }
        };

        var result = Workout.FromData(data);

        result.Id.Should().Be(data.id);
        result.AddDateTime.Should().Be(data.addDateTime);
        result.Exercises.Should().HaveCount(1);
        result.Exercises[0].ExerciseInfoId.Should().Be(exerciseInfoId);
    }

    [Fact]
    public void FromData_WithNullExercises_ReturnsEmptyList()
    {
        var data = new DataTransferObjects.Common.Workout
        {
            id = Guid.NewGuid(),
            addDateTime = DateTime.UtcNow,
            exercises = null!
        };

        var result = Workout.FromData(data);

        result.Exercises.Should().BeEmpty();
    }
}
