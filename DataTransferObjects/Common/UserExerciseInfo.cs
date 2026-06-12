using System;

namespace WorkoutTracker.Server.DataTransferObjects.Common;

public class UserExerciseInfo
{
    public required ExerciseInfo exerciseInfo { get; set; }

    // Best set, rep x wieght
    public decimal? bestSetWeight { get; set; }
    public int? bestSetReps { get; set; }

    public ExerciseTarget? target { get; set; }
}
