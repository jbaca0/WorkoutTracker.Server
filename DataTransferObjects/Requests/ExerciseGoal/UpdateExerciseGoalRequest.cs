using System;

namespace WorkoutTracker.Server.DataTransferObjects.Requests.ExerciseGoal;

public class UpdateExerciseGoalRequest
{
    public decimal startWeight { get; set; }
    public int startReps { get; set; }
    public decimal targetWeight { get; set; }
    public int targetReps { get; set; }
    public DateTime goalDate { get; set; }
}
