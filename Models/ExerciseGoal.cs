using System;

namespace WorkoutTracker.Server.Models;

public class ExerciseGoal
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid ExerciseInfoId { get; set; }
    public ExerciseInfo ExerciseInfo { get; set; } = null!;
    public Guid WorkoutPlanId { get; set; }
    public WorkoutPlan WorkoutPlan { get; set; } = null!;
    public decimal StartWeight { get; set; }
    public int StartReps { get; set; }
    public decimal TargetWeight { get; set; }
    public int TargetReps { get; set; }
    public DateTime GoalDate { get; set; }
    public DateTime CreatedAt { get; set; }
}
