using System;

namespace WorkoutTracker.Server.DomainObjects;

public class UserExerciseInfo
{
    public required ExerciseInfo ExerciseInfo { get; set; }
    public Exercise? LatestExercise { get; set; }
    public decimal? BestWeight { get; set; }
    public int? MaxReps { get; set; }
    public DateTime? LastPerformed { get; set; }
    public int TotalSessions { get; set; }

    public void CalculateBestStats()
    {
        if (LatestExercise?.Sets != null && LatestExercise.Sets.Any())
        {
            BestWeight = LatestExercise.Sets.MaxBy(s => s.Weight)?.Weight;
            MaxReps = LatestExercise.Sets.MaxBy(s => s.Reps)?.Reps;
        }
    }
}