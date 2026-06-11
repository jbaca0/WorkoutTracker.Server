using FluentAssertions;
using Microsoft.AspNetCore.Http.HttpResults;
using WorkoutTracker.Server.DataTransferObjects.Responses.Logging;
using WorkoutTracker.Server.Handlers;
using WorkoutTracker.Server.Models.Repositories;
using WorkoutTracker.Server.Tests.Helpers;
using Xunit;

namespace WorkoutTracker.Server.Tests.Handlers;

public class LogHandlerTests
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

    // --- GetLogs ---
    // Note: LogHandler.DeleteAllLogs delegates to ExecuteDeleteAsync, which the
    // EF InMemory provider does not support, so it is not unit tested here.

    [Fact]
    public async Task GetLogs_ReturnsAllLogsWithTotalCount()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        dbContext.LogEntries.AddRange(MakeLog(message: "first"), MakeLog(message: "second"));
        await dbContext.SaveChangesAsync();

        var result = await LogHandler.GetLogs(new LogEntryRepository(dbContext));

        var ok = result.Should().BeOfType<Ok<GetLogsResponse>>().Subject;
        ok.Value!.logs.Should().HaveCount(2);
        ok.Value.totalCount.Should().Be(2);
    }

    [Fact]
    public async Task GetLogs_WithLevelFilter_ReturnsOnlyMatchingLogs()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        dbContext.LogEntries.AddRange(
            MakeLog(level: "Error", message: "boom"),
            MakeLog(level: "Information", message: "fine"));
        await dbContext.SaveChangesAsync();

        var result = await LogHandler.GetLogs(new LogEntryRepository(dbContext), level: "Error");

        var ok = result.Should().BeOfType<Ok<GetLogsResponse>>().Subject;
        var log = ok.Value!.logs.Should().ContainSingle().Subject;
        log.logLevel.Should().Be("Error");
        ok.Value.totalCount.Should().Be(1);
    }

    [Fact]
    public async Task GetLogs_WithLimitAndOffset_PagesResultsButReportsFullCount()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        for (var i = 0; i < 5; i++)
        {
            dbContext.LogEntries.Add(MakeLog(message: $"log {i}", timestamp: DateTime.UtcNow.AddMinutes(-i)));
        }
        await dbContext.SaveChangesAsync();

        var result = await LogHandler.GetLogs(new LogEntryRepository(dbContext), limit: 2, offset: 1);

        var ok = result.Should().BeOfType<Ok<GetLogsResponse>>().Subject;
        ok.Value!.logs.Should().HaveCount(2);
        ok.Value.totalCount.Should().Be(5);
    }
}
