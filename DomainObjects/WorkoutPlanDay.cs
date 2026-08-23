namespace WorkoutTracker.Server.DomainObjects;

public class WorkoutPlanDay
{
    public Guid Id { get; set; }
    public Guid WorkoutPlanId { get; set; }
    public int DayOfWeek { get; set; }
    public Guid? WorkoutTemplateId { get; set; }
    public WorkoutTemplate? WorkoutTemplate { get; set; }
    public string? Note { get; set; }

    public static WorkoutPlanDay FromModel(Models.WorkoutPlanDay model)
    {
        return new WorkoutPlanDay
        {
            Id = model.Id,
            WorkoutPlanId = model.WorkoutPlanId,
            DayOfWeek = model.DayOfWeek,
            WorkoutTemplateId = model.WorkoutTemplateId,
            WorkoutTemplate = model.WorkoutTemplate != null ? WorkoutTemplate.FromModel(model.WorkoutTemplate) : null,
            Note = model.Note
        };
    }

    public DataTransferObjects.Common.WorkoutPlanDay ToData()
    {
        return new DataTransferObjects.Common.WorkoutPlanDay
        {
            id = Id,
            dayOfWeek = DayOfWeek,
            workoutTemplateId = WorkoutTemplateId,
            templateName = WorkoutTemplate?.Name,
            note = Note
        };
    }
}
