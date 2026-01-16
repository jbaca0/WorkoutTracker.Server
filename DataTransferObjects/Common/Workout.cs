using System;

namespace WorkoutTracker.Server.DataTransferObjects.Common;

public class Workout
{
    public Guid id { get; set; }
    public List<Exercise> exercises { get; set; } = new List<Exercise>();

    public DateTime addDateTime { get; set; }
}
