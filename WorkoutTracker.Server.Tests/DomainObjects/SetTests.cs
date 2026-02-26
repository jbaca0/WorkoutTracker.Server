using FluentAssertions;
using WorkoutTracker.Server.DomainObjects;
using Xunit;

namespace WorkoutTracker.Server.Tests.DomainObjects;

public class SetTests
{
    [Fact]
    public void FromModel_MapsAllProperties()
    {
        var model = new Models.Set
        {
            Id = Guid.NewGuid(),
            ExerciseId = Guid.NewGuid(),
            Reps = 10,
            Weight = 135.5m,
            Completed = true
        };

        var result = Set.FromModel(model);

        result.Id.Should().Be(model.Id);
        result.ExerciseId.Should().Be(model.ExerciseId);
        result.Reps.Should().Be(10);
        result.Weight.Should().Be(135.5m);
        result.Completed.Should().BeTrue();
    }

    [Fact]
    public void ToModel_MapsAllProperties()
    {
        var set = new Set
        {
            Id = Guid.NewGuid(),
            ExerciseId = Guid.NewGuid(),
            Reps = 8,
            Weight = 225m,
            Completed = false
        };

        var result = set.ToModel();

        result.Id.Should().Be(set.Id);
        result.ExerciseId.Should().Be(set.ExerciseId);
        result.Reps.Should().Be(8);
        result.Weight.Should().Be(225m);
        result.Completed.Should().BeFalse();
    }

    [Fact]
    public void FromData_MapsRepsWeightCompleted()
    {
        var data = new DataTransferObjects.Common.Set
        {
            id = Guid.NewGuid(),
            reps = 12,
            weight = 50m,
            completed = true
        };

        var result = Set.FromData(data);

        result.Reps.Should().Be(12);
        result.Weight.Should().Be(50m);
        result.Completed.Should().BeTrue();
    }

    [Fact]
    public void ToData_MapsAllProperties()
    {
        var set = new Set
        {
            Id = Guid.NewGuid(),
            ExerciseId = Guid.NewGuid(),
            Reps = 5,
            Weight = 315m,
            Completed = true
        };

        var result = set.ToData();

        result.id.Should().Be(set.Id);
        result.reps.Should().Be(5);
        result.weight.Should().Be(315m);
        result.completed.Should().BeTrue();
    }

    [Fact]
    public void RoundTrip_FromModelToModel_PreservesData()
    {
        var original = new Models.Set
        {
            Id = Guid.NewGuid(),
            ExerciseId = Guid.NewGuid(),
            Reps = 6,
            Weight = 185m,
            Completed = false
        };

        var result = Set.FromModel(original).ToModel();

        result.Id.Should().Be(original.Id);
        result.ExerciseId.Should().Be(original.ExerciseId);
        result.Reps.Should().Be(original.Reps);
        result.Weight.Should().Be(original.Weight);
        result.Completed.Should().Be(original.Completed);
    }
}
