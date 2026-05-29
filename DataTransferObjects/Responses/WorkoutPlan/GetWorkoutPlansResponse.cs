namespace WorkoutTracker.Server.DataTransferObjects.Responses.WorkoutPlan;

public class GetWorkoutPlansResponse
{
    public List<Common.WorkoutPlan> plans { get; set; } = new();
}
