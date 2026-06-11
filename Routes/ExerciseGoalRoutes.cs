using WorkoutTracker.Server.Handlers;

namespace WorkoutTracker.Server.Routes;

public static class ExerciseGoalRoutes
{
    public static void MapExerciseGoalRoutes(this WebApplication app)
    {
        app.MapGet("/exerciseGoals", ExerciseGoalHandler.GetGoals)
            .RequireAuthorization();

        app.MapPost("/exerciseGoals", ExerciseGoalHandler.CreateGoal)
            .RequireAuthorization();

        app.MapPut("/exerciseGoals/{id}", ExerciseGoalHandler.UpdateGoal)
            .RequireAuthorization();

        app.MapDelete("/exerciseGoals/{id}", ExerciseGoalHandler.DeleteGoal)
            .RequireAuthorization();
    }
}
