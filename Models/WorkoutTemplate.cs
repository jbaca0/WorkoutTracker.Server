namespace WorkoutTracker.Server.Models;

public class WorkoutTemplate
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public DateTime CreatedDateTime { get; set; }
    public ICollection<WorkoutTemplateExercise> Exercises { get; set; } = new List<WorkoutTemplateExercise>();
}
