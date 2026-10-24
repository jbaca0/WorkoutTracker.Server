using System;

namespace WorkoutTracker.Server.DataTransferObjects.Requests.ExerciseGoal;

public class AssignGoalToPlanRequest
{
    public Guid? workoutPlanId { get; set; }
}
