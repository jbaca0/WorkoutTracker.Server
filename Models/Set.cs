using System;

namespace WorkoutTracker.Server.Models;

public class Set
{
    public Guid Id { get; set; }

    public Guid ExerciseId { get; set; }
    public Exercise Exercise { get; set; } = null!;

    public int Reps { get; set; }
    public decimal Weight { get; set; }
}
