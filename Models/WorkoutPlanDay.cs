namespace WorkoutTracker.Server.Models;

public class WorkoutPlanDay
{
    public Guid Id { get; set; }
    public Guid WorkoutPlanId { get; set; }
    public WorkoutPlan WorkoutPlan { get; set; } = null!;
    public int DayOfWeek { get; set; } // 0=Sunday, 1=Monday, ..., 6=Saturday
    public Guid? WorkoutTemplateId { get; set; }
    public WorkoutTemplate? WorkoutTemplate { get; set; }
    public string? Note { get; set; }
}
