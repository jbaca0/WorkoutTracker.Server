namespace WorkoutTracker.Server.DataTransferObjects.Requests.WorkoutPlan;

public class UpdateWorkoutPlanRequest
{
    public Guid planId { get; set; }
    public string name { get; set; } = string.Empty;
    public List<WorkoutPlanDayRequest> days { get; set; } = new();
}
