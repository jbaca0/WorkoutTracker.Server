using System;
using WorkoutTracker.Server.DataTransferObjects.Common;

namespace WorkoutTracker.Server.DataTransferObjects.Responses;

public class GetExerciseInfosResponse
{
    public List<ExerciseInfo> exerciseInfos { get; set; } = new List<ExerciseInfo>();
}
