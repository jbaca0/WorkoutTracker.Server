using System;

namespace WorkoutTracker.Server.DataTransferObjects.Common;

public class Set
{
    public Guid? id { get; set; }
    public int reps { get; set; }
    public decimal weight { get; set; }
    public bool completed { get; set; }
    public int order { get; set; }

    public static Set BuildFromModel(Models.Set set)
    {
        return new Set()
        {
            id = set.Id,
            reps = set.Reps,
            weight = set.Weight,
            completed = set.Completed,
            order = set.Order
        };
    }
}
