using System;
using WorkoutTracker.Server.DataTransferObjects.Common;

namespace WorkoutTracker.Server.DataTransferObjects.Responses.Workout;

public class GetWorkoutHistoryResponse
{
    public List<WorkoutSummary> workouts { get; set; } = new List<WorkoutSummary>();
}
