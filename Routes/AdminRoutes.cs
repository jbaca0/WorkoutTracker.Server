using WorkoutTracker.Server.Handlers;

namespace WorkoutTracker.Server.Routes;

public static class AdminRoutes
{
    public static void MapAdminRoutes(this WebApplication app)
    {
        var group = app.MapGroup("/admin")
            .RequireAuthorization("AdminOnly");

        group.MapGet("/users", AdminHandler.GetUsers)
            .WithOpenApi();

        group.MapGet("/roles", AdminHandler.GetRoles)
            .WithOpenApi();

        group.MapGet("/users/{userId}/roles", AdminHandler.GetUserRoles)
            .WithOpenApi();

        group.MapPut("/users/{userId}/roles", AdminHandler.UpdateUserRoles)
            .WithOpenApi();

        group.MapPost("/users", AdminHandler.CreateUser)
            .WithOpenApi();

        group.MapDelete("/users/{userId}", AdminHandler.DeleteUser)
            .WithOpenApi();

        group.MapGet("/exerciseInfos", AdminHandler.GetExerciseInfos)
            .WithOpenApi();

        group.MapPost("/exerciseInfos", AdminHandler.CreateExerciseInfo)
            .WithOpenApi();

        group.MapPut("/exerciseInfos/{id}", AdminHandler.UpdateExerciseInfo)
            .WithOpenApi();

        group.MapDelete("/exerciseInfos/{id}", AdminHandler.DeleteExerciseInfo)
            .WithOpenApi();

        group.MapGet("/logs", LogHandler.GetLogs)
            .WithOpenApi();

        group.MapDelete("/logs", LogHandler.DeleteAllLogs)
            .WithOpenApi();
    }
}
