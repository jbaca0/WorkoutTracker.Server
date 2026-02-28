using System;
using WorkoutTracker.Server.Handlers;

namespace WorkoutTracker.Server.Routes;

public static class ExerciseInfoRoutes
{
    public static void MapExerciseInfoRoutes(this WebApplication app)
    {
        app.MapPost("/addExerciseInfo", ExerciseInfoHandler.AddExerciseInfo)
            .RequireAuthorization();
        app.MapGet("/getExerciseInfos", ExerciseInfoHandler.GetExerciseInfos)
            .RequireAuthorization();
    }
}
