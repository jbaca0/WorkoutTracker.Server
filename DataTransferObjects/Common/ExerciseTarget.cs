using WorkoutTracker.Server.Enums;

namespace WorkoutTracker.Server.DataTransferObjects.Common;

public class ExerciseTarget
{
    public TargetSource type { get; set; }
    public List<TargetSet> sets { get; set; } = new List<TargetSet>();
}
