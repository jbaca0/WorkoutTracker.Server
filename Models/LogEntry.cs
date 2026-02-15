using System;

namespace WorkoutTracker.Server.Models;

public class LogEntry
{
    public Guid Id { get; set; }
    public DateTime Timestamp { get; set; }
    public string LogLevel { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Exception { get; set; }
}
