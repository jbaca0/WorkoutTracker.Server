using System;

namespace WorkoutTracker.Server.DataTransferObjects.Responses.Workout;

public class GetWorkoutDetailResponse
{
    public Common.Workout workout { get; set; } = null!;
}
