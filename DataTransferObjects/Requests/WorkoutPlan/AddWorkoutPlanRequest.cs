namespace WorkoutTracker.Server.DataTransferObjects.Requests.WorkoutPlan;

public class AddWorkoutPlanRequest
{
    public string name { get; set; } = string.Empty;
    public List<WorkoutPlanDayRequest> days { get; set; } = new();
}

public class WorkoutPlanDayRequest
{
    public int dayOfWeek { get; set; }
    public Guid? workoutTemplateId { get; set; }
    public string? note { get; set; }
}
