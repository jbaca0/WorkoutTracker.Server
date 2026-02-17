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
        string? search = null,
        int limit = 100,
        int offset = 0)
    {
        var (logs, totalCount) = await logEntryRepository.GetLogsAsync(level, startDate, endDate, search, limit, offset);

        var response = new GetLogsResponse
        {
            logs = logs.Select(l => l.ToData()).ToList(),
            totalCount = totalCount
        };

        return Results.Ok(response);
    }

    public static async Task<IResult> DeleteAllLogs(LogEntryRepository logEntryRepository)
    {
        await logEntryRepository.DeleteAllAsync();
        return Results.Ok();
    }
}
