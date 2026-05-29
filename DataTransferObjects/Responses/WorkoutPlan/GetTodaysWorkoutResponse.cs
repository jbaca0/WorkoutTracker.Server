namespace WorkoutTracker.Server.DataTransferObjects.Responses.WorkoutPlan;

public class GetTodaysWorkoutResponse
{
    public Common.WorkoutTemplate? template { get; set; }
    public string? note { get; set; }
}
