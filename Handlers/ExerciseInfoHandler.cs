using System;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using WorkoutTracker.Server.DataTransferObjects.Requests;
using WorkoutTracker.Server.DataTransferObjects.Responses;
using WorkoutTracker.Server.DomainObjects;
using WorkoutTracker.Server.Enums;
using WorkoutTracker.Server.Extensions;
using WorkoutTracker.Server.Models.Repositories;

namespace WorkoutTracker.Server.Handlers;

public class ExerciseInfoHandler
{
    public static async Task<IResult> AddExerciseInfo(
        ClaimsPrincipal currentUser,
        ExerciseInfoRepository exerciseInfoRepository,
        AddExerciseInfoRequest request)
    {
        Guid? createdByUserId = currentUser.TryGetUserId(out Guid userId) ? userId : null;
        var exerciseInfo = new ExerciseInfo()
        {
            Name = request.name,
            PrimaryMuscleGroup = request.primaryMuscleGroup,
            SecondaryMuscleGroups = request.secondaryMuscleGroups,
            CreatedByUserId = createdByUserId
        };
        await exerciseInfoRepository.Add(exerciseInfo);
        return Results.Ok();
    }

    internal static async Task<IResult> GetExerciseInfos(
        ClaimsPrincipal user,
        ExerciseInfoRepository exerciseInfoRepository)
    {
        var exerciseInfos = user.TryGetUserId(out Guid userId)
            ? await exerciseInfoRepository.GetForUserAsync(userId)
            : await exerciseInfoRepository.GetAllAsync();
        var result = new GetExerciseInfosResponse
        {
            exerciseInfos = exerciseInfos.Select(ei => ei.ToData()).ToList()
        };
        return Results.Ok(result);
    }
}
