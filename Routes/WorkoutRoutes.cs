using System;
using WorkoutTracker.Server.Authorization.EndpointFilters;
using WorkoutTracker.Server.Handlers;
using WorkoutTracker.Server.Models;

namespace WorkoutTracker.Server.Routes;

public static class WorkoutRoutes
{
    public static void MapWorkoutRoutes(this WebApplication app)
    {
        app.MapGet("/getWorkouts", WorkoutHandler.GetWorkouts)
            .RequireAuthorization();

        app.MapPost("/addWorkout", WorkoutHandler.AddWorkout)
            .RequireAuthorization();

        var workoutOwnerGroup = app.MapGroup("")
            .RequireAuthorization()
            .AddEndpointFilter<WorkoutOwnerFilter>();

        workoutOwnerGroup.MapGet("/getExercises/{workoutId}", WorkoutHandler.GetExercises);

        workoutOwnerGroup.MapPost("/addExercise", WorkoutHandler.AddExercise);

        workoutOwnerGroup.MapPost("/addExercises", WorkoutHandler.AddExercises);

        workoutOwnerGroup.MapPost("/save", WorkoutHandler.Save);

    }
}
