using System;

namespace WorkoutTracker.Server.DataTransferObjects.Common;

public class Set
{
    public int reps { get; set; }
    public decimal weight { get; set; }

    public static Set BuildFromModel(Models.Set set)
    {
        return new Set()
        {
            reps = set.Reps,
            weight = set.Weight
        };
    }
}
