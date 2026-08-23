namespace WorkoutTracker.Server.DataTransferObjects.Responses.ExerciseGoal;

public class GetExerciseGoalsResponse
{
    public List<Common.ExerciseGoal> goals { get; set; } = new();
}
