namespace WorkoutTracker.Server.DataTransferObjects.Common;

public class WorkoutTemplate
{
    public Guid id { get; set; }
    public string name { get; set; } = string.Empty;
    public List<WorkoutTemplateExercise> exercises { get; set; } = new();
}
