namespace WorkoutTracker.Server.Routes;

public static class MonitoringRoutes
{
    public static void MapMonitoringRoutes(this WebApplication app)
    {
        var group = app.MapGroup("/monitoring");

        // Deliberately anonymous: this is the only unauthenticated endpoint, and it
        // returns a constant so it cannot disclose anything about the instance.
        group.MapGet("/status", () => Results.Ok(new { status = "running" }))
            .WithOpenApi();

        // There is intentionally no log endpoint here. Logs are served by
        // GET /admin/logs, which requires the AdminOnly policy.
    }
}
