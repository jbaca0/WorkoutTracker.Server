using FluentAssertions;
using Domain = WorkoutTracker.Server.DomainObjects;
using WorkoutTracker.Server.Models.Repositories;
using WorkoutTracker.Server.Tests.Helpers;
using Xunit;

namespace WorkoutTracker.Server.Tests.Repositories;

public class LogEntryRepositoryTests
{
    private static Models.LogEntry MakeLog(
        string level = "Information",
        string message = "message",
        string category = "category",
        DateTime? timestamp = null) => new()
        {
            Id = Guid.NewGuid(),
            LogLevel = level,
            Message = message,
            Category = category,
            Timestamp = timestamp ?? DateTime.UtcNow
        };

    // --- AddAsync ---

    [Fact]
    public async Task AddAsync_PersistsLogEntry()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var repo = new LogEntryRepository(dbContext);
        var entry = new Domain.LogEntry
        {
            Id = Guid.NewGuid(),
            Timestamp = DateTime.UtcNow,
            LogLevel = "Warning",
            Category = "Tests",
            Message = "Something happened",
            Exception = "boom"
        };

        await repo.AddAsync(entry);

        var stored = dbContext.LogEntries.Should().ContainSingle().Subject;
        stored.Id.Should().Be(entry.Id);
        stored.LogLevel.Should().Be("Warning");
        stored.Message.Should().Be("Something happened");
        stored.Exception.Should().Be("boom");
    }

    // --- GetLogsAsync ---
    // Note: DeleteAllAsync uses ExecuteDeleteAsync, which the EF InMemory
    // provider does not support, so it is not unit tested here.

    [Fact]
    public async Task GetLogsAsync_WithNoFilters_ReturnsAllLogsAndTotalCount()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        dbContext.LogEntries.AddRange(MakeLog(message: "one"), MakeLog(message: "two"));
        await dbContext.SaveChangesAsync();

        var repo = new LogEntryRepository(dbContext);
        var (logs, totalCount) = await repo.GetLogsAsync();

        logs.Should().HaveCount(2);
        totalCount.Should().Be(2);
    }

    [Fact]
    public async Task GetLogsAsync_FiltersByLevel()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        dbContext.LogEntries.AddRange(
            MakeLog(level: "Error", message: "bad"),
            MakeLog(level: "Information", message: "fine"));
        await dbContext.SaveChangesAsync();

        var repo = new LogEntryRepository(dbContext);
        var (logs, totalCount) = await repo.GetLogsAsync(level: "Error");

        logs.Should().ContainSingle().Which.Message.Should().Be("bad");
        totalCount.Should().Be(1);
    }

    [Fact]
    public async Task GetLogsAsync_FiltersByDateRange()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var baseTime = new DateTime(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc);
        dbContext.LogEntries.AddRange(
            MakeLog(message: "too old", timestamp: baseTime.AddDays(-10)),
            MakeLog(message: "in range", timestamp: baseTime),
            MakeLog(message: "too new", timestamp: baseTime.AddDays(10)));
        await dbContext.SaveChangesAsync();

        var repo = new LogEntryRepository(dbContext);
        var (logs, totalCount) = await repo.GetLogsAsync(
            startDate: baseTime.AddDays(-1),
            endDate: baseTime.AddDays(1));

        logs.Should().ContainSingle().Which.Message.Should().Be("in range");
        totalCount.Should().Be(1);
    }

    [Fact]
    public async Task GetLogsAsync_SearchMatchesMessageOrCategory()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        dbContext.LogEntries.AddRange(
            MakeLog(message: "database timeout occurred", category: "EFCore"),
            MakeLog(message: "all good", category: "database connection"),
            MakeLog(message: "unrelated", category: "Other"));
        await dbContext.SaveChangesAsync();

        var repo = new LogEntryRepository(dbContext);
        var (logs, totalCount) = await repo.GetLogsAsync(search: "database");

        logs.Should().HaveCount(2);
        totalCount.Should().Be(2);
    }

    [Fact]
    public async Task GetLogsAsync_ReturnsLogsOrderedByTimestampDescending()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var baseTime = DateTime.UtcNow;
        dbContext.LogEntries.AddRange(
            MakeLog(message: "oldest", timestamp: baseTime.AddHours(-2)),
            MakeLog(message: "newest", timestamp: baseTime),
            MakeLog(message: "middle", timestamp: baseTime.AddHours(-1)));
        await dbContext.SaveChangesAsync();

        var repo = new LogEntryRepository(dbContext);
        var (logs, _) = await repo.GetLogsAsync();

        logs.Select(l => l.Message).Should().ContainInOrder("newest", "middle", "oldest");
    }

    [Fact]
    public async Task GetLogsAsync_AppliesLimitAndOffsetWhileReportingTotalCount()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var baseTime = DateTime.UtcNow;
        for (var i = 0; i < 5; i++)
        {
            dbContext.LogEntries.Add(MakeLog(message: $"log {i}", timestamp: baseTime.AddMinutes(-i)));
        }
        await dbContext.SaveChangesAsync();

        var repo = new LogEntryRepository(dbContext);
        var (logs, totalCount) = await repo.GetLogsAsync(limit: 2, offset: 1);

        logs.Should().HaveCount(2);
        logs.Select(l => l.Message).Should().ContainInOrder("log 1", "log 2");
        totalCount.Should().Be(5);
    }
}
