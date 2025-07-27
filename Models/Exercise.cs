using System;

namespace WorkoutTracker.Server.Models;

public class Exercise
{
    public Guid Id { get; set; }

    public Guid WorkoutId { get; set; }
    public Workout Workout { get; set; } = null!;

    public Guid ExerciseInfoId { get; set; }
    public ExerciseInfo ExerciseInfo { get; set; } = null!;

    public ICollection<Set> Sets { get; set; } = new List<Set>();
}
