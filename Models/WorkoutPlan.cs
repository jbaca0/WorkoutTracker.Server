namespace WorkoutTracker.Server.Models;

public class WorkoutPlan
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedDateTime { get; set; }
    public ICollection<WorkoutPlanDay> Days { get; set; } = new List<WorkoutPlanDay>();
}
