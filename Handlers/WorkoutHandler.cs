using System;
using WorkoutTracker.Server.Extensions;
using WorkoutTracker.Server.Models;

namespace WorkoutTracker.Server.Handlers;

public static class WorkoutHandler
{
    public static async Task<IResult> AddWorkout(HttpContext context, ApplicationDbContext dbContext)
    {
        var userId = context.User.GetUserId();
        if (userId == null)
        {
            return Results.Unauthorized();
        }

        var workout = new Workout();
        workout.UserId = userId.Value;
        dbContext.Workouts.Add(workout);
        await dbContext.SaveChangesAsync();
        return Results.Ok(workout.Id);
    }
}
