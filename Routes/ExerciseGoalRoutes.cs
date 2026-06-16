using WorkoutTracker.Server.Handlers;

namespace WorkoutTracker.Server.Routes;

public static class ExerciseGoalRoutes
{
    public static void MapExerciseGoalRoutes(this WebApplication app)
    {
        app.MapGet("/exerciseGoals", ExerciseGoalHandler.GetGoals)
            .RequireAuthorization();

        app.MapGet("/exerciseGoals/plan/{planId}", ExerciseGoalHandler.GetGoalsByPlan)
            .RequireAuthorization();

        app.MapPost("/exerciseGoals", ExerciseGoalHandler.CreateGoal)
            .RequireAuthorization();

        app.MapPut("/exerciseGoals/{id}", ExerciseGoalHandler.UpdateGoal)
            .RequireAuthorization();

        app.MapPut("/exerciseGoals/{id}/plan", ExerciseGoalHandler.AssignGoalToPlan)
            .RequireAuthorization();

        app.MapDelete("/exerciseGoals/{id}", ExerciseGoalHandler.DeleteGoal)
            .RequireAuthorization();
    }
}
