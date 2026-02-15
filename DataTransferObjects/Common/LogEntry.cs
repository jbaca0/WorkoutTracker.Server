using System;

namespace WorkoutTracker.Server.DataTransferObjects.Common;

public class LogEntry
{
    public Guid id { get; set; }
    public DateTime timestamp { get; set; }
    public string logLevel { get; set; } = string.Empty;
    public string category { get; set; } = string.Empty;
    public string message { get; set; } = string.Empty;
    public string? exception { get; set; }
}
