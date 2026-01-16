using System;
using Microsoft.EntityFrameworkCore;
using WorkoutTracker.Server.DataTransferObjects.Requests;
using WorkoutTracker.Server.DataTransferObjects.Responses;
using WorkoutTracker.Server.Models;

namespace WorkoutTracker.Server.Handlers;

public class ExerciseInfoHandler
{
    public static async Task<IResult> AddExerciseInfo(ApplicationDbContext dbContext, AddExerciseInfoRequest request)
    {
        var exerciseInfo = new ExerciseInfo()
        {
            Name = request.name
        };
        dbContext.ExerciseInfos.Add(exerciseInfo);
        await dbContext.SaveChangesAsync();
        return Results.Ok();
    }

    internal static async Task<IResult> GetExerciseInfos(ApplicationDbContext dbContext)
    {
        var exerciseInfos = await dbContext.ExerciseInfos.ToListAsync();
        var result = new GetExerciseInfosResponse
        {
            exerciseInfos = exerciseInfos.Select(ei => new DataTransferObjects.Common.ExerciseInfo
            {
                name = ei.Name,
                id = ei.Id
            }).ToList()
        };
        return Results.Ok(result);
    }
}
