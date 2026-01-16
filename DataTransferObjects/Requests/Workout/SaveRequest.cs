using System;
using WorkoutTracker.Server.DataTransferObjects.Common;

namespace WorkoutTracker.Server.DataTransferObjects.Requests;

public class SaveRequest : IWorkoutRequest
{
    public Guid workoutId { get; set; }
    public required Workout workout { get; set; }

}
