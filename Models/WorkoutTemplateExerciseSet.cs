namespace WorkoutTracker.Server.Models;

public class WorkoutTemplateExerciseSet
{
    public Guid Id { get; set; }
    public Guid WorkoutTemplateExerciseId { get; set; }
    public WorkoutTemplateExercise WorkoutTemplateExercise { get; set; } = null!;
    public int Order { get; set; }
    public int Reps { get; set; }
}
