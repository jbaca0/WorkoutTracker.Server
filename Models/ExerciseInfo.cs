using System;

namespace WorkoutTracker.Server.Models;

public class ExerciseInfo
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
