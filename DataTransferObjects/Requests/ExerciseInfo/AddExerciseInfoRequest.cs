using System;
using System.Collections.Generic;
using WorkoutTracker.Server.Enums;

namespace WorkoutTracker.Server.DataTransferObjects.Requests;

public class AddExerciseInfoRequest
{
    public string name { get; set; } = string.Empty;
    public MuscleGroup primaryMuscleGroup { get; set; } = MuscleGroup.Other;
    public List<MuscleGroup> secondaryMuscleGroups { get; set; } = new();
}
