using System;

namespace WorkoutTracker.Server.DataTransferObjects.Requests;

public interface IWorkoutRequest
{
    public Guid workoutId { get; }
}
