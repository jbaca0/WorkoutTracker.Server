namespace WorkoutTracker.Server.DataTransferObjects.Responses.WorkoutTemplate;

public class GetTemplatesResponse
{
    public List<Common.WorkoutTemplate> templates { get; set; } = new();
}
