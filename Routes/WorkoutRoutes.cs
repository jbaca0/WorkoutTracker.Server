using System;
using WorkoutTracker.Server.EndpointHandlers;
using WorkoutTracker.Server.Models;

namespace WorkoutTracker.Server.Routes;

public static class WorkoutRoutes
{
    public static void MapWorkoutRoutes(this WebApplication app)
    {
        app.MapPost("/addWorkout", WorkoutHandler.AddWorkout)
        .RequireAuthorization();
    }
}
