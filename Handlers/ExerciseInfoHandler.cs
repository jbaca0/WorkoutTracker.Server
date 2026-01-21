using System;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using WorkoutTracker.Server.DataTransferObjects.Requests;
using WorkoutTracker.Server.DataTransferObjects.Responses;
using WorkoutTracker.Server.DomainObjects;
using WorkoutTracker.Server.Enums;

using WorkoutTracker.Server.Models.Repositories;

namespace WorkoutTracker.Server.Handlers;

public class ExerciseInfoHandler
{
    public static async Task<IResult> AddExerciseInfo(ExerciseInfoRepository exerciseInfoRepository, AddExerciseInfoRequest request)
    {

        var exerciseInfo = new ExerciseInfo()
        {
            Name = request.name,
            PrimaryMuscleGroup = request.primaryMuscleGroup,
            SecondaryMuscleGroups = request.secondaryMuscleGroups
        };
        await exerciseInfoRepository.Add(exerciseInfo);
        return Results.Ok();
    }

    internal static async Task<IResult> GetExerciseInfos(ExerciseInfoRepository exerciseInfoRepository)
    {
        var exerciseInfos = await exerciseInfoRepository.GetAllAsync();
        var result = new GetExerciseInfosResponse
        {
            exerciseInfos = exerciseInfos.Select(ei => ei.ToData()).ToList()
        };
        return Results.Ok(result);
    }
}
