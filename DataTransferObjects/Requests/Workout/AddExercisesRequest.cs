using System;

namespace WorkoutTracker.Server.DataTransferObjects.Requests;

public class AddExercisesRequest : IWorkoutRequest
{
    public Guid workoutId { get; set; }
    public List<Guid>? exerciseInfoIds { get; set; }
}
