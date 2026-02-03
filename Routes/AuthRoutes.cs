using WorkoutTracker.Server.Handlers;

namespace WorkoutTracker.Server.Routes;

public static class AuthRoutes
{
    public static void MapAuthRoutes(this WebApplication app)
    {
        app.MapPost("/auth/login", AuthHandler.Login)
            .WithOpenApi();

        app.MapPost("/auth/refresh", AuthHandler.Refresh)
            .WithOpenApi();

        app.MapPost("/auth/logout", AuthHandler.Logout)
            .RequireAuthorization()
            .WithOpenApi();
    }
}
