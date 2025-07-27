using System;
using WorkoutTracker.Server.Handlers;

namespace WorkoutTracker.Server.Routes;

public static class ExerciseInfoRoutes
{
    public static void MapExerciseInfoRoutes(this WebApplication app)
    {
        // Map the route for adding exercise information
        app.MapPost("/addExerciseInfo", ExerciseInfoHandler.AddExerciseInfo);
    }
}
