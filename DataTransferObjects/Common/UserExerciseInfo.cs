using System;

namespace WorkoutTracker.Server.DataTransferObjects.Common;

public class UserExerciseInfo
{
    public required ExerciseInfo exerciseInfo { get; set; }
    public Exercise? latestExercise { get; set; }

    // Best set, rep x wieght 
}
