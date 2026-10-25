using FluentAssertions;
using WorkoutTracker.Server.DomainObjects;
using Xunit;

namespace WorkoutTracker.Server.Tests.DomainObjects;

public class LogEntryTests
{
    private static LogEntry MakeDomain() => new()
    {
        Id = Guid.NewGuid(),
        Timestamp = new DateTime(2026, 6, 1, 8, 30, 0, DateTimeKind.Utc),
        LogLevel = "Error",
        Category = "WorkoutTracker.Tests",
        Message = "Something failed",
        Exception = "System.Exception: boom"
    };

    [Fact]
    public void ToModel_MapsAllProperties()
    {
        var domain = MakeDomain();

        var model = domain.ToModel();

        model.Id.Should().Be(domain.Id);
        model.Timestamp.Should().Be(domain.Timestamp);
        model.LogLevel.Should().Be(domain.LogLevel);
        model.Category.Should().Be(domain.Category);
        model.Message.Should().Be(domain.Message);
        model.Exception.Should().Be(domain.Exception);
    }

    [Fact]
    public void FromModel_MapsAllProperties()
    {
        var model = new Models.LogEntry
        {
            Id = Guid.NewGuid(),
            Timestamp = new DateTime(2026, 6, 2, 9, 0, 0, DateTimeKind.Utc),
            LogLevel = "Warning",
            Category = "Category",
            Message = "Message",
            Exception = null
        };

        var domain = LogEntry.FromModel(model);

        domain.Id.Should().Be(model.Id);
        domain.Timestamp.Should().Be(model.Timestamp);
        domain.LogLevel.Should().Be(model.LogLevel);
        domain.Category.Should().Be(model.Category);
        domain.Message.Should().Be(model.Message);
        domain.Exception.Should().BeNull();
    }

    [Fact]
    public void ToData_MapsAllProperties()
    {
        var domain = MakeDomain();

        var dto = domain.ToData();

        dto.id.Should().Be(domain.Id);
        dto.timestamp.Should().Be(domain.Timestamp);
        dto.logLevel.Should().Be(domain.LogLevel);
        dto.category.Should().Be(domain.Category);
        dto.message.Should().Be(domain.Message);
        dto.exception.Should().Be(domain.Exception);
    }

    [Fact]
    public void FromModel_ThenToModel_RoundTripsAllValues()
    {
        var original = MakeDomain().ToModel();

        var roundTripped = LogEntry.FromModel(original).ToModel();

        roundTripped.Should().BeEquivalentTo(original);
    }
}
