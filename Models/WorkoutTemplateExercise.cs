namespace WorkoutTracker.Server.Models;

public class WorkoutTemplateExercise
{
    public Guid Id { get; set; }
    public Guid WorkoutTemplateId { get; set; }
    public WorkoutTemplate WorkoutTemplate { get; set; } = null!;
    public Guid ExerciseInfoId { get; set; }
    public ExerciseInfo ExerciseInfo { get; set; } = null!;
    public int Order { get; set; }
    public int TargetReps { get; set; }
}
