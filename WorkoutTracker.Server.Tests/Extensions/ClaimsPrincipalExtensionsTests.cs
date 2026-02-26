using FluentAssertions;
using WorkoutTracker.Server.Extensions;
using WorkoutTracker.Server.Tests.Helpers;
using Xunit;

namespace WorkoutTracker.Server.Tests.Extensions;

public class ClaimsPrincipalExtensionsTests
{
    // --- GetUserId ---

    [Fact]
    public void GetUserId_WithValidGuidClaim_ReturnsGuid()
    {
        var userId = Guid.NewGuid();
        var principal = ClaimsPrincipalBuilder.WithUserId(userId);

        var result = principal.GetUserId();

        result.Should().Be(userId);
    }

    [Fact]
    public void GetUserId_WithMissingClaim_ThrowsException()
    {
        var principal = ClaimsPrincipalBuilder.WithNoUserId();

        var act = () => principal.GetUserId();

        act.Should().Throw<Exception>().WithMessage("UserId Not Found");
    }

    [Fact]
    public void GetUserId_WithInvalidGuidClaim_ThrowsException()
    {
        var principal = ClaimsPrincipalBuilder.WithInvalidUserId();

        var act = () => principal.GetUserId();

        act.Should().Throw<Exception>().WithMessage("UserId Not Found");
    }

    // --- TryGetUserId ---

    [Fact]
    public void TryGetUserId_WithValidGuidClaim_ReturnsTrueAndGuid()
    {
        var userId = Guid.NewGuid();
        var principal = ClaimsPrincipalBuilder.WithUserId(userId);

        var success = principal.TryGetUserId(out var result);

        success.Should().BeTrue();
        result.Should().Be(userId);
    }

    [Fact]
    public void TryGetUserId_WithMissingClaim_ReturnsFalseAndDefaultGuid()
    {
        var principal = ClaimsPrincipalBuilder.WithNoUserId();

        var success = principal.TryGetUserId(out var result);

        success.Should().BeFalse();
        result.Should().Be(default(Guid));
    }

    [Fact]
    public void TryGetUserId_WithInvalidGuidClaim_ReturnsFalseAndDefaultGuid()
    {
        var principal = ClaimsPrincipalBuilder.WithInvalidUserId();

        var success = principal.TryGetUserId(out var result);

        success.Should().BeFalse();
        result.Should().Be(default(Guid));
    }
}
