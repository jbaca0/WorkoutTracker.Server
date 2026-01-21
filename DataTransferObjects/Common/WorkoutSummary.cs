using System;

namespace WorkoutTracker.Server.DataTransferObjects.Common;

public class WorkoutSummary
{
    public Guid id { get; set; }
    public DateTime addDateTime { get; set; }
    public int exerciseCount { get; set; }
    public List<string> exerciseNames { get; set; } = new List<string>();
}
