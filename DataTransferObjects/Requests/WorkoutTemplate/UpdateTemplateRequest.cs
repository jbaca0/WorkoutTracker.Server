namespace WorkoutTracker.Server.DataTransferObjects.Requests.WorkoutTemplate;

public class UpdateTemplateRequest
{
    public Guid templateId { get; set; }
    public string name { get; set; } = string.Empty;
    public List<WorkoutTemplateExerciseRequest> exercises { get; set; } = new();
}
