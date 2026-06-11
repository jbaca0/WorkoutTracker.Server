namespace WorkoutTracker.Server.DataTransferObjects.Common;

public class WorkoutTemplateExercise
{
    public Guid id { get; set; }
    public Guid exerciseInfoId { get; set; }
    public ExerciseInfo? exerciseInfo { get; set; }
    public int order { get; set; }
    public int targetReps { get; set; }
}
