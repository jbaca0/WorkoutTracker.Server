using System;
using System.Collections.Generic;
using WorkoutTracker.Server.Enums;

namespace WorkoutTracker.Server.DataTransferObjects.Common;

public class ExerciseInfo
{
    public string name { get; set; } = string.Empty;
    public Guid id { get; set; } = Guid.Empty;
    public MuscleGroup primaryMuscleGroup { get; set; } = MuscleGroup.None;
    public List<string> secondaryMuscleGroups { get; set; } = new();
    public Guid? createdByUserId { get; set; }
}
