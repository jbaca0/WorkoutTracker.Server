using System;

namespace WorkoutTracker.Server.DomainObjects;

public class LogEntry
{
    public Guid Id { get; set; }
    public DateTime Timestamp { get; set; }
    public string LogLevel { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Exception { get; set; }

    public Models.LogEntry ToModel()
    {
        return new Models.LogEntry
        {
            Id = Id,
            Timestamp = Timestamp,
            LogLevel = LogLevel,
            Category = Category,
            Message = Message,
            Exception = Exception
        };
    }

    public static LogEntry FromModel(Models.LogEntry model)
    {
        return new LogEntry
        {
            Id = model.Id,
            Timestamp = model.Timestamp,
            LogLevel = model.LogLevel,
            Category = model.Category,
            Message = model.Message,
            Exception = model.Exception
        };
    }
}
