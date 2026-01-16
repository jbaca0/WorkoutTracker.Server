using System;

namespace WorkoutTracker.Server.DataTransferObjects.Requests;

public class AddExerciseRequest
{
    public Guid workoutId { get; set; }
    public Guid exerciseInfoId { get; set; }
}
