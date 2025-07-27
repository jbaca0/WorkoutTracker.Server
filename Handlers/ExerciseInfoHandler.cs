using System;
using WorkoutTracker.Server.DataTransferObjects.Requests;
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
}
