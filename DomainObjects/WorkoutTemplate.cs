namespace WorkoutTracker.Server.DomainObjects;

public class WorkoutTemplate
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public DateTime CreatedDateTime { get; set; }
    public List<WorkoutTemplateExercise> Exercises { get; set; } = new();

    public static WorkoutTemplate FromModel(Models.WorkoutTemplate model)
    {
        return new WorkoutTemplate
        {
            Id = model.Id,
            Name = model.Name,
            UserId = model.UserId,
            CreatedDateTime = model.CreatedDateTime,
            Exercises = model.Exercises?.Select(WorkoutTemplateExercise.FromModel).ToList() ?? new()
        };
    }

    public Models.WorkoutTemplate ToModel()
    {
        return new Models.WorkoutTemplate
        {
            Id = Id,
            Name = Name,
            UserId = UserId,
            CreatedDateTime = CreatedDateTime,
            Exercises = Exercises.Select(e => e.ToModel()).ToList()
        };
    }

    public DataTransferObjects.Common.WorkoutTemplate ToData()
    {
        return new DataTransferObjects.Common.WorkoutTemplate
        {
            id = Id,
            name = Name,
            exercises = Exercises.Select(e => e.ToData()).ToList()
        };
    }
}
