namespace WorkoutTracker.Server.DataTransferObjects.Responses.WorkoutTemplate;

public class StartWorkoutFromTemplateResponse
{
    public Guid workoutId { get; set; }
    public List<Common.UserExerciseInfo> userExerciseInfos { get; set; } = new();
}
