using System;
using Microsoft.Extensions.DependencyInjection;
using WorkoutTracker.Server.Models;

namespace WorkoutTracker.Server.Logging;

public class DatabaseLogger : ILogger
{
    private readonly string _category;
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;

    public DatabaseLogger(string category, IServiceProvider serviceProvider, IConfiguration configuration)
    {
        _category = category;
        _serviceProvider = serviceProvider;
        _configuration = configuration;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel)
    {
        var configuredLevel = _configuration.GetValue<string>("DatabaseLogging:MinimumLevel");
        if (Enum.TryParse<LogLevel>(configuredLevel, ignoreCase: true, out var minimumLevel))
        {
            return logLevel >= minimumLevel;
        }
        return logLevel >= LogLevel.Warning;
    }

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
        {
            return;
        }

        var message = formatter(state, exception);

        Task.Run(async () =>
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var logEntry = new LogEntry
                {
                    Id = Guid.NewGuid(),
                    Timestamp = DateTime.UtcNow,
                    LogLevel = logLevel.ToString(),
                    Category = _category,
                    Message = message,
                    Exception = exception?.ToString()
                };

                dbContext.LogEntries.Add(logEntry);
                await dbContext.SaveChangesAsync();
            }
            catch
            {
                // Swallow exceptions to prevent logging failures from crashing the app
            }
        });
    }
}
