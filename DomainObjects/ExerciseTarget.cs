using WorkoutTracker.Server.Enums;

namespace WorkoutTracker.Server.DomainObjects;

public class ExerciseTarget
{
    public TargetSource Type { get; set; }
    public List<TargetSet> Sets { get; set; } = new List<TargetSet>();

    public DataTransferObjects.Common.ExerciseTarget ToData()
    {
        return new DataTransferObjects.Common.ExerciseTarget
        {
            type = Type,
            sets = Sets.Select(s => s.ToData()).ToList()
        };
    }
}
