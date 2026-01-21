using System;
using WorkoutTracker.Server.Enums;

namespace WorkoutTracker.Server.Models;

public class ExerciseInfo
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public MuscleGroup? PrimaryMuscleGroup { get; set; } = MuscleGroup.Other;
    public string? SecondaryMuscleGroupsJson { get; set; }
}
