using System;
using WorkoutTracker.Server.DataTransferObjects.Common;

namespace WorkoutTracker.Server.DataTransferObjects.Responses;

public class AddExercisesResponse
{
    public required List<UserExerciseInfo> userExerciseInfos { get; set; }
}
