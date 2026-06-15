namespace WorkoutTracker.Server.DomainObjects;

public class TargetSet
{
    public decimal Weight { get; set; }
    public int Reps { get; set; }

    public DataTransferObjects.Common.TargetSet ToData()
    {
        return new DataTransferObjects.Common.TargetSet
        {
            weight = Weight,
            reps = Reps
        };
    }
}
