using System;

namespace WorkoutTracker.Server.DataTransferObjects.Requests.ExerciseGoal;

public class CreateExerciseGoalRequest
{
    public Guid exerciseInfoId { get; set; }
    public Guid workoutPlanId { get; set; }
    public decimal? startWeight { get; set; }
    public int? startReps { get; set; }
    public decimal targetWeight { get; set; }
    public int targetReps { get; set; }
    public DateTime goalDate { get; set; }
}
