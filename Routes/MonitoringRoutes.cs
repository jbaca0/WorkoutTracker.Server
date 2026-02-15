using WorkoutTracker.Server.Handlers;

namespace WorkoutTracker.Server.Routes;

public static class MonitoringRoutes
{
    public static void MapMonitoringRoutes(this WebApplication app)
    {
        var group = app.MapGroup("/monitoring");

        group.MapGet("/status", () => Results.Ok(new { status = "running" }))
            .WithOpenApi();

        group.MapGet("/logs", LogHandler.GetLogs)
            .WithOpenApi();
    }
}
