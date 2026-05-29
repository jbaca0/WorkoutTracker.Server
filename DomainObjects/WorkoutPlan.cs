namespace WorkoutTracker.Server.DomainObjects;

public class WorkoutPlan
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedDateTime { get; set; }
    public List<WorkoutPlanDay> Days { get; set; } = new();

    public static WorkoutPlan FromModel(Models.WorkoutPlan model)
    {
        return new WorkoutPlan
        {
            Id = model.Id,
            UserId = model.UserId,
            Name = model.Name,
            IsActive = model.IsActive,
            CreatedDateTime = model.CreatedDateTime,
            Days = model.Days?.Select(WorkoutPlanDay.FromModel).ToList() ?? new()
        };
    }

    public DataTransferObjects.Common.WorkoutPlan ToData()
    {
        return new DataTransferObjects.Common.WorkoutPlan
        {
            id = Id,
            name = Name,
            isActive = IsActive,
            days = Days.Select(d => d.ToData()).ToList()
        };
    }
}
