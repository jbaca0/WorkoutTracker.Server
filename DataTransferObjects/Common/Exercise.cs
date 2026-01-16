using System;


namespace WorkoutTracker.Server.DataTransferObjects.Common;

public class Exercise
{
    public Guid exerciseInfoId { get; set; }
    public List<Set> sets { get; set; } = new List<Set>();

    public static Exercise BuildFromModel(Models.Exercise exercise)
    {
        return new Exercise()
        {
            exerciseInfoId = exercise.ExerciseInfoId,
            sets = exercise.Sets.Select(s => Set.BuildFromModel(s)).ToList()
        };
    }
}
