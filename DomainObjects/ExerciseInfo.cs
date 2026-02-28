using System;
using System.Collections.Generic;
using System.Text.Json;
using WorkoutTracker.Server.Enums;

namespace WorkoutTracker.Server.DomainObjects;

public class ExerciseInfo
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public MuscleGroup? PrimaryMuscleGroup { get; set; } = MuscleGroup.Other;
    public List<MuscleGroup>? SecondaryMuscleGroups { get; set; } = new();
    public Guid? CreatedByUserId { get; set; }

    public static ExerciseInfo FromModel(Models.ExerciseInfo model)
    {
        var secondaryMuscleGroups = new List<MuscleGroup>();
        if (!string.IsNullOrEmpty(model.SecondaryMuscleGroupsJson))
        {
            var jsonArray = JsonSerializer.Deserialize<List<int>>(model.SecondaryMuscleGroupsJson);
            if (jsonArray != null)
            {
                secondaryMuscleGroups = jsonArray.Select(i => (MuscleGroup)i).ToList();
            }
        }

        return new ExerciseInfo
        {
            Id = model.Id,
            Name = model.Name,
            PrimaryMuscleGroup = model.PrimaryMuscleGroup,
            SecondaryMuscleGroups = secondaryMuscleGroups,
            CreatedByUserId = model.CreatedByUserId
        };
    }

    public Models.ExerciseInfo ToModel()
    {
        var secondaryJson = SecondaryMuscleGroups != null ? JsonSerializer.Serialize(SecondaryMuscleGroups.Select(m => (int)m).ToList()) : null;

        return new Models.ExerciseInfo
        {
            Id = Id,
            Name = Name,
            PrimaryMuscleGroup = PrimaryMuscleGroup,
            SecondaryMuscleGroupsJson = secondaryJson,
            CreatedByUserId = CreatedByUserId
        };
    }

    public static ExerciseInfo FromData(DataTransferObjects.Common.ExerciseInfo data)
    {
        return new ExerciseInfo
        {
            Id = data.id,
            Name = data.name,
            PrimaryMuscleGroup = data.primaryMuscleGroup,
            SecondaryMuscleGroups = data.secondaryMuscleGroups?.Select(m => Enum.TryParse<MuscleGroup>(m, out var group) ? group : MuscleGroup.Other).ToList() ?? new List<MuscleGroup>(),
            CreatedByUserId = data.createdByUserId
        };
    }

    public DataTransferObjects.Common.ExerciseInfo ToData()
    {
        return new DataTransferObjects.Common.ExerciseInfo
        {
            id = Id,
            name = Name,
            primaryMuscleGroup = PrimaryMuscleGroup ?? MuscleGroup.None,
            secondaryMuscleGroups = SecondaryMuscleGroups?.Select(m => m.ToString()).ToList() ?? new List<string>(),
            createdByUserId = CreatedByUserId
        };
    }
}
