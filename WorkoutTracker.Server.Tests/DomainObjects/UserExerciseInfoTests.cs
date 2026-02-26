using FluentAssertions;
using WorkoutTracker.Server.DomainObjects;
using WorkoutTracker.Server.Enums;
using Xunit;

namespace WorkoutTracker.Server.Tests.DomainObjects;

public class UserExerciseInfoTests
{
    private static ExerciseInfo MakeExerciseInfo() => new()
    {
        Id = Guid.NewGuid(),
        Name = "Bench Press",
        PrimaryMuscleGroup = MuscleGroup.Chest
    };

    [Fact]
    public void CalculateBestStats_WithMultipleSets_SetsBestWeightAndMaxReps()
    {
        var exerciseInfo = MakeExerciseInfo();
        var exerciseId = Guid.NewGuid();
        var uei = new UserExerciseInfo
        {
            ExerciseInfo = exerciseInfo,
            LatestExercise = new Exercise
            {
                Id = exerciseId,
                ExerciseInfo = exerciseInfo,
                Sets = new List<Set>
                {
                    new() { Id = Guid.NewGuid(), Weight = 135m, Reps = 10 },
                    new() { Id = Guid.NewGuid(), Weight = 185m, Reps = 5 },
                    new() { Id = Guid.NewGuid(), Weight = 155m, Reps = 8 }
                }
            }
        };

        uei.CalculateBestStats();

        uei.BestWeight.Should().Be(185m);
        uei.MaxReps.Should().Be(10);
    }

    [Fact]
    public void CalculateBestStats_WithSingleSet_SetsCorrectValues()
    {
        var exerciseInfo = MakeExerciseInfo();
        var uei = new UserExerciseInfo
        {
            ExerciseInfo = exerciseInfo,
            LatestExercise = new Exercise
            {
                Id = Guid.NewGuid(),
                ExerciseInfo = exerciseInfo,
                Sets = new List<Set>
                {
                    new() { Id = Guid.NewGuid(), Weight = 225m, Reps = 3 }
                }
            }
        };

        uei.CalculateBestStats();

        uei.BestWeight.Should().Be(225m);
        uei.MaxReps.Should().Be(3);
    }

    [Fact]
    public void CalculateBestStats_WithNullLatestExercise_DoesNotSetStats()
    {
        var uei = new UserExerciseInfo
        {
            ExerciseInfo = MakeExerciseInfo(),
            LatestExercise = null
        };

        uei.CalculateBestStats();

        uei.BestWeight.Should().BeNull();
        uei.MaxReps.Should().BeNull();
    }

    [Fact]
    public void CalculateBestStats_WithNullSets_DoesNotSetStats()
    {
        var exerciseInfo = MakeExerciseInfo();
        var uei = new UserExerciseInfo
        {
            ExerciseInfo = exerciseInfo,
            LatestExercise = new Exercise
            {
                Id = Guid.NewGuid(),
                ExerciseInfo = exerciseInfo,
                Sets = null!
            }
        };

        uei.CalculateBestStats();

        uei.BestWeight.Should().BeNull();
        uei.MaxReps.Should().BeNull();
    }

    [Fact]
    public void CalculateBestStats_WithEmptySets_DoesNotSetStats()
    {
        var exerciseInfo = MakeExerciseInfo();
        var uei = new UserExerciseInfo
        {
            ExerciseInfo = exerciseInfo,
            LatestExercise = new Exercise
            {
                Id = Guid.NewGuid(),
                ExerciseInfo = exerciseInfo,
                Sets = new List<Set>()
            }
        };

        uei.CalculateBestStats();

        uei.BestWeight.Should().BeNull();
        uei.MaxReps.Should().BeNull();
    }
}
