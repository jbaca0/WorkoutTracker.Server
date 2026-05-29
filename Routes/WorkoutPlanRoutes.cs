using WorkoutTracker.Server.Handlers;

namespace WorkoutTracker.Server.Routes;

public static class WorkoutPlanRoutes
{
    public static void MapWorkoutPlanRoutes(this WebApplication app)
    {
        app.MapGet("/getWorkoutPlans", WorkoutPlanHandler.GetWorkoutPlans)
            .RequireAuthorization();

        app.MapPost("/addWorkoutPlan", WorkoutPlanHandler.AddWorkoutPlan)
            .RequireAuthorization();

        app.MapPut("/updateWorkoutPlan", WorkoutPlanHandler.UpdateWorkoutPlan)
            .RequireAuthorization();

        app.MapDelete("/deleteWorkoutPlan/{planId}", WorkoutPlanHandler.DeleteWorkoutPlan)
            .RequireAuthorization();

        app.MapPut("/activateWorkoutPlan/{planId}", WorkoutPlanHandler.ActivateWorkoutPlan)
            .RequireAuthorization();

        app.MapGet("/getTodaysWorkout", WorkoutPlanHandler.GetTodaysWorkout)
            .RequireAuthorization();
    }
}
