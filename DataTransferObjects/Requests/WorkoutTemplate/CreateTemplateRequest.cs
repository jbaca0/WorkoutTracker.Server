namespace WorkoutTracker.Server.DataTransferObjects.Requests.WorkoutTemplate;

public class CreateTemplateRequest
{
    public string name { get; set; } = string.Empty;
    public List<WorkoutTemplateExerciseRequest> exercises { get; set; } = new();
}

public class WorkoutTemplateExerciseRequest
{
    public Guid exerciseInfoId { get; set; }
    public List<WorkoutTemplateExerciseSetRequest> sets { get; set; } = new();
}

public class WorkoutTemplateExerciseSetRequest
{
    public int reps { get; set; }
}
