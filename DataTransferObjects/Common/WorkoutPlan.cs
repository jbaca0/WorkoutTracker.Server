namespace WorkoutTracker.Server.DataTransferObjects.Common;

public class WorkoutPlan
{
    public Guid id { get; set; }
    public string name { get; set; } = string.Empty;
    public bool isActive { get; set; }
    public List<WorkoutPlanDay> days { get; set; } = new();
}
