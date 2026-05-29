namespace WorkoutTracker.Server.DataTransferObjects.Requests.WorkoutTemplate;

public class CreateTemplateRequest
{
    public string name { get; set; } = string.Empty;
    public List<Guid> exerciseInfoIds { get; set; } = new();
}
