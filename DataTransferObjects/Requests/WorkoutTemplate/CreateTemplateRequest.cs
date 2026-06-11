namespace WorkoutTracker.Server.DataTransferObjects.Requests.WorkoutTemplate;

public class CreateTemplateRequest
{
    public string name { get; set; } = string.Empty;
    public List<WorkoutTemplateExerciseRequest> exercises { get; set; } = new();
}

public class WorkoutTemplateExerciseRequest
{
    public Guid exerciseInfoId { get; set; }
    public int targetReps { get; set; }
}
