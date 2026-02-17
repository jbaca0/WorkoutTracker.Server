using System;
using Microsoft.EntityFrameworkCore;

namespace WorkoutTracker.Server.Models.Repositories;

public class LogEntryRepository
{
    private ApplicationDbContext dbContext;

    public LogEntryRepository(ApplicationDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task AddAsync(DomainObjects.LogEntry logEntry)
    {
        dbContext.LogEntries.Add(logEntry.ToModel());
        await dbContext.SaveChangesAsync();
    }

    public async Task<(List<DomainObjects.LogEntry> Logs, int TotalCount)> GetLogsAsync(
        string? level = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        string? search = null,
        int limit = 100,
        int offset = 0)
    {
        limit = Math.Min(limit, 1000);

        var query = dbContext.LogEntries.AsQueryable();

        if (!string.IsNullOrEmpty(level))
        {
            query = query.Where(l => l.LogLevel == level);
        }

        if (startDate.HasValue)
        {
            query = query.Where(l => l.Timestamp >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(l => l.Timestamp <= endDate.Value);
        }

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(l =>
                EF.Functions.Like(l.Message, $"%{search}%") ||
                EF.Functions.Like(l.Category, $"%{search}%"));
        }

        var totalCount = await query.CountAsync();

        var logs = await query
            .OrderByDescending(l => l.Timestamp)
            .Skip(offset)
            .Take(limit)
            .ToListAsync();

        return (logs.Select(DomainObjects.LogEntry.FromModel).ToList(), totalCount);
    }

    public async Task<int> DeleteAllAsync()
    {
        return await dbContext.LogEntries.ExecuteDeleteAsync();
    }
}
