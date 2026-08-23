namespace WorkoutTracker.Server.DataTransferObjects.Common;

public class WorkoutPlanDay
{
    public Guid id { get; set; }
    public int dayOfWeek { get; set; }
    public Guid? workoutTemplateId { get; set; }
    public string? templateName { get; set; }
    public string? note { get; set; }
}
