using WorkoutTracker.Server.Enums;

namespace WorkoutTracker.Server.DataTransferObjects.Requests.Admin;

public class UpdateExerciseInfoRequest
{
    public string Name { get; set; } = string.Empty;
    public MuscleGroup PrimaryMuscleGroup { get; set; } = MuscleGroup.Other;
    public List<MuscleGroup> SecondaryMuscleGroups { get; set; } = [];
}
