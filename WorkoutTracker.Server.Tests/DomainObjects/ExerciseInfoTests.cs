using FluentAssertions;
using WorkoutTracker.Server.DomainObjects;
using WorkoutTracker.Server.Enums;
using Xunit;

namespace WorkoutTracker.Server.Tests.DomainObjects;

public class ExerciseInfoTests
{
    // --- FromModel ---

    [Fact]
    public void FromModel_WithSecondaryMuscleGroups_DeserializesJson()
    {
        var model = new Models.ExerciseInfo
        {
            Id = Guid.NewGuid(),
            Name = "Bench Press",
            PrimaryMuscleGroup = MuscleGroup.Chest,
            SecondaryMuscleGroupsJson = "[2, 4]" // Shoulders, Triceps
        };

        var result = ExerciseInfo.FromModel(model);

        result.Id.Should().Be(model.Id);
        result.Name.Should().Be("Bench Press");
        result.PrimaryMuscleGroup.Should().Be(MuscleGroup.Chest);
        result.SecondaryMuscleGroups.Should().BeEquivalentTo(new[] { MuscleGroup.Shoulders, MuscleGroup.Triceps });
    }

    [Fact]
    public void FromModel_WithNullSecondaryMuscleGroupsJson_ReturnsEmptyList()
    {
        var model = new Models.ExerciseInfo
        {
            Id = Guid.NewGuid(),
            Name = "Squat",
            PrimaryMuscleGroup = MuscleGroup.Legs,
            SecondaryMuscleGroupsJson = null
        };

        var result = ExerciseInfo.FromModel(model);

        result.SecondaryMuscleGroups.Should().BeEmpty();
    }

    [Fact]
    public void FromModel_WithEmptySecondaryMuscleGroupsJson_ReturnsEmptyList()
    {
        var model = new Models.ExerciseInfo
        {
            Id = Guid.NewGuid(),
            Name = "Squat",
            PrimaryMuscleGroup = MuscleGroup.Legs,
            SecondaryMuscleGroupsJson = ""
        };

        var result = ExerciseInfo.FromModel(model);

        result.SecondaryMuscleGroups.Should().BeEmpty();
    }

    [Fact]
    public void FromModel_WithEmptyJsonArray_ReturnsEmptyList()
    {
        var model = new Models.ExerciseInfo
        {
            Id = Guid.NewGuid(),
            Name = "Squat",
            PrimaryMuscleGroup = MuscleGroup.Legs,
            SecondaryMuscleGroupsJson = "[]"
        };

        var result = ExerciseInfo.FromModel(model);

        result.SecondaryMuscleGroups.Should().BeEmpty();
    }

    // --- ToModel ---

    [Fact]
    public void ToModel_WithSecondaryMuscleGroups_SerializesToJson()
    {
        var domain = new ExerciseInfo
        {
            Id = Guid.NewGuid(),
            Name = "Overhead Press",
            PrimaryMuscleGroup = MuscleGroup.Shoulders,
            SecondaryMuscleGroups = new List<MuscleGroup> { MuscleGroup.Triceps, MuscleGroup.Core }
        };

        var result = domain.ToModel();

        result.Id.Should().Be(domain.Id);
        result.Name.Should().Be("Overhead Press");
        result.PrimaryMuscleGroup.Should().Be(MuscleGroup.Shoulders);
        result.SecondaryMuscleGroupsJson.Should().Be("[4,6]"); // Triceps=4, Core=6
    }

    [Fact]
    public void ToModel_WithNullSecondaryMuscleGroups_ProducesNullJson()
    {
        var domain = new ExerciseInfo
        {
            Id = Guid.NewGuid(),
            Name = "Pull Up",
            PrimaryMuscleGroup = MuscleGroup.Back,
            SecondaryMuscleGroups = null
        };

        var result = domain.ToModel();

        result.SecondaryMuscleGroupsJson.Should().BeNull();
    }

    [Fact]
    public void ToModel_WithEmptySecondaryMuscleGroups_ProducesEmptyJsonArray()
    {
        var domain = new ExerciseInfo
        {
            Id = Guid.NewGuid(),
            Name = "Pull Up",
            PrimaryMuscleGroup = MuscleGroup.Back,
            SecondaryMuscleGroups = new List<MuscleGroup>()
        };

        var result = domain.ToModel();

        result.SecondaryMuscleGroupsJson.Should().Be("[]");
    }

    // --- Round-trip ---

    [Fact]
    public void RoundTrip_FromModelToModel_PreservesData()
    {
        var original = new Models.ExerciseInfo
        {
            Id = Guid.NewGuid(),
            Name = "Deadlift",
            PrimaryMuscleGroup = MuscleGroup.Back,
            SecondaryMuscleGroupsJson = "[7,6]" // Legs=7, Core=6
        };

        var result = ExerciseInfo.FromModel(original).ToModel();

        result.Id.Should().Be(original.Id);
        result.Name.Should().Be(original.Name);
        result.PrimaryMuscleGroup.Should().Be(original.PrimaryMuscleGroup);
        result.SecondaryMuscleGroupsJson.Should().Be(original.SecondaryMuscleGroupsJson);
    }

    // --- FromData ---

    [Fact]
    public void FromData_WithValidEnumStrings_ParsesCorrectly()
    {
        var data = new DataTransferObjects.Common.ExerciseInfo
        {
            id = Guid.NewGuid(),
            name = "Curl",
            primaryMuscleGroup = MuscleGroup.Biceps,
            secondaryMuscleGroups = new List<string> { "Forearms", "Core" }
        };

        var result = ExerciseInfo.FromData(data);

        result.Id.Should().Be(data.id);
        result.Name.Should().Be("Curl");
        result.PrimaryMuscleGroup.Should().Be(MuscleGroup.Biceps);
        result.SecondaryMuscleGroups.Should().BeEquivalentTo(new[] { MuscleGroup.Forearms, MuscleGroup.Core });
    }

    [Fact]
    public void FromData_WithUnknownEnumString_FallsBackToOther()
    {
        var data = new DataTransferObjects.Common.ExerciseInfo
        {
            id = Guid.NewGuid(),
            name = "Mystery Move",
            primaryMuscleGroup = MuscleGroup.None,
            secondaryMuscleGroups = new List<string> { "NotAMuscle" }
        };

        var result = ExerciseInfo.FromData(data);

        result.SecondaryMuscleGroups.Should().BeEquivalentTo(new[] { MuscleGroup.Other });
    }

    [Fact]
    public void FromData_WithNullSecondaryMuscleGroups_ReturnsEmptyList()
    {
        var data = new DataTransferObjects.Common.ExerciseInfo
        {
            id = Guid.NewGuid(),
            name = "Plank",
            primaryMuscleGroup = MuscleGroup.Core,
            secondaryMuscleGroups = null!
        };

        var result = ExerciseInfo.FromData(data);

        result.SecondaryMuscleGroups.Should().BeEmpty();
    }
}
