using System;
using System.Collections.Concurrent;

namespace WorkoutTracker.Server.Logging;

public class DatabaseLoggerProvider : ILoggerProvider
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private readonly ConcurrentDictionary<string, DatabaseLogger> _loggers = new();

    public DatabaseLoggerProvider(IServiceProvider serviceProvider, IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _configuration = configuration;
    }

    public ILogger CreateLogger(string categoryName)
    {
        return _loggers.GetOrAdd(categoryName, name => new DatabaseLogger(name, _serviceProvider, _configuration));
    }

    public void Dispose()
    {
        _loggers.Clear();
    }
}
