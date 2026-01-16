using System;

namespace WorkoutTracker.Server.DataTransferObjects.Common;

public class ExerciseInfo
{
    public string name { get; set; } = string.Empty;
    public Guid id { get; set; } = Guid.Empty;

    public static ExerciseInfo BuildFromModel(Models.ExerciseInfo exerciseInfo)
    {
        return new ExerciseInfo()
        {
            id = exerciseInfo.Id,
            name = exerciseInfo.Name
        };
    }
}
