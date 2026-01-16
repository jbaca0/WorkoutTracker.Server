using System;

namespace WorkoutTracker.Server.DataTransferObjects.Requests;

public class GetExercisesRequest : IWorkoutRequest
{
    public required Guid workoutId { get; set; }
}
