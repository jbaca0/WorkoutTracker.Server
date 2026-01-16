using System;

namespace WorkoutTracker.Server.DataTransferObjects.Requests;

public class AddExerciseInfoRequest
{
    public string name { get; set; } = string.Empty;
}
