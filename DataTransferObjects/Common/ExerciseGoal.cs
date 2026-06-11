using System;

namespace WorkoutTracker.Server.DataTransferObjects.Common;

public class ExerciseGoal
{
    public Guid id { get; set; }
    public Guid exerciseInfoId { get; set; }
    public ExerciseInfo? exerciseInfo { get; set; }
    public Guid workoutPlanId { get; set; }
    public decimal startWeight { get; set; }
    public int startReps { get; set; }
    public decimal targetWeight { get; set; }
    public int targetReps { get; set; }
    public DateTime goalDate { get; set; }
    public GoalRecommendation? goalRecommendation { get; set; }
}
