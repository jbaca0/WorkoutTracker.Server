using System;
using System.Diagnostics;

namespace WorkoutTracker.Server.Middleware;

public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value;

        // Skip logging requests to the logs endpoint to avoid noise
        if (path != null && path.StartsWith("/monitoring/logs", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        var stopwatch = Stopwatch.StartNew();

        try
        {
            await _next(context);
            stopwatch.Stop();

            _logger.LogInformation(
                "{RequestMethod} {RequestPath} responded {StatusCode} in {DurationMs}ms",
                context.Request.Method,
                path,
                context.Response.StatusCode,
                stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            _logger.LogError(
                ex,
                "{RequestMethod} {RequestPath} threw an exception after {DurationMs}ms",
                context.Request.Method,
                path,
                stopwatch.ElapsedMilliseconds);

            context.Response.StatusCode = 500;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new { error = "An internal server error occurred." });
        }
    }
}
