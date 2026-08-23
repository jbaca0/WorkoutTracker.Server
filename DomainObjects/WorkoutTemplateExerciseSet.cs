namespace WorkoutTracker.Server.DomainObjects;

public class WorkoutTemplateExerciseSet
{
    public Guid Id { get; set; }
    public int Order { get; set; }
    public int Reps { get; set; }

    public static WorkoutTemplateExerciseSet FromModel(Models.WorkoutTemplateExerciseSet model)
    {
        return new WorkoutTemplateExerciseSet
        {
            Id = model.Id,
            Order = model.Order,
            Reps = model.Reps
        };
    }

    public DataTransferObjects.Common.WorkoutTemplateExerciseSet ToData()
    {
        return new DataTransferObjects.Common.WorkoutTemplateExerciseSet
        {
            id = Id,
            order = Order,
            reps = Reps
        };
    }
}
