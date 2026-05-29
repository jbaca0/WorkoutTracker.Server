namespace WorkoutTracker.Server.DomainObjects;

public class WorkoutTemplateExercise
{
    public Guid Id { get; set; }
    public Guid WorkoutTemplateId { get; set; }
    public Guid ExerciseInfoId { get; set; }
    public ExerciseInfo? ExerciseInfo { get; set; }
    public int Order { get; set; }

    public static WorkoutTemplateExercise FromModel(Models.WorkoutTemplateExercise model)
    {
        return new WorkoutTemplateExercise
        {
            Id = model.Id,
            WorkoutTemplateId = model.WorkoutTemplateId,
            ExerciseInfoId = model.ExerciseInfoId,
            ExerciseInfo = model.ExerciseInfo != null ? DomainObjects.ExerciseInfo.FromModel(model.ExerciseInfo) : null,
            Order = model.Order
        };
    }

    public Models.WorkoutTemplateExercise ToModel()
    {
        return new Models.WorkoutTemplateExercise
        {
            Id = Id,
            WorkoutTemplateId = WorkoutTemplateId,
            ExerciseInfoId = ExerciseInfoId,
            Order = Order
        };
    }

    public DataTransferObjects.Common.WorkoutTemplateExercise ToData()
    {
        return new DataTransferObjects.Common.WorkoutTemplateExercise
        {
            id = Id,
            exerciseInfoId = ExerciseInfoId,
            exerciseInfo = ExerciseInfo != null ? new DataTransferObjects.Common.ExerciseInfo
            {
                id = ExerciseInfo.Id,
                name = ExerciseInfo.Name
            } : null,
            order = Order
        };
    }
}
