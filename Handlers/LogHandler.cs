using System;
using WorkoutTracker.Server.DataTransferObjects.Responses.Logging;
using WorkoutTracker.Server.Models.Repositories;

namespace WorkoutTracker.Server.Handlers;

public static class LogHandler
{
    public static async Task<IResult> GetLogs(
        LogEntryRepository logEntryRepository,
        string? level = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        int limit = 100,
        int offset = 0)
    {
        var (logs, totalCount) = await logEntryRepository.GetLogsAsync(level, startDate, endDate, limit, offset);

        var response = new GetLogsResponse
        {
            logs = logs.Select(l => new DataTransferObjects.Common.LogEntry
            {
                id = l.Id,
                timestamp = l.Timestamp,
                logLevel = l.LogLevel,
                category = l.Category,
                message = l.Message,
                exception = l.Exception
            }).ToList(),
            totalCount = totalCount
        };

        return Results.Ok(response);
    }
}
