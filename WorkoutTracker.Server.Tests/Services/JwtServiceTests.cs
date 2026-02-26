using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using WorkoutTracker.Server.Services;
using Xunit;

namespace WorkoutTracker.Server.Tests.Services;

public class JwtServiceTests
{
    private static JwtService CreateService(int accessTokenMinutes = 60, int refreshTokenDays = 7)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Secret"] = "super-secret-key-that-is-long-enough-for-hmac-sha256",
                ["Jwt:Issuer"] = "test-issuer",
                ["Jwt:Audience"] = "test-audience",
                ["Jwt:AccessTokenExpirationMinutes"] = accessTokenMinutes.ToString(),
                ["Jwt:RefreshTokenExpirationDays"] = refreshTokenDays.ToString()
            })
            .Build();
        return new JwtService(config);
    }

    private static IdentityUser MakeUser() => new()
    {
        Id = Guid.NewGuid().ToString(),
        Email = "test@example.com",
        UserName = "testuser"
    };

    // --- GenerateAccessToken ---

    [Fact]
    public void GenerateAccessToken_ReturnsNonEmptyToken()
    {
        var service = CreateService();
        var user = MakeUser();

        var token = service.GenerateAccessToken(user, new List<string>());

        token.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void GenerateAccessToken_TokenContainsUserIdClaim()
    {
        var service = CreateService();
        var user = MakeUser();

        var token = service.GenerateAccessToken(user, new List<string>());

        var handler = new JwtSecurityTokenHandler();
        var parsed = handler.ReadJwtToken(token);
        var nameIdClaim = parsed.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
        nameIdClaim.Should().NotBeNull();
        nameIdClaim!.Value.Should().Be(user.Id);
    }

    [Fact]
    public void GenerateAccessToken_TokenContainsRoleClaims()
    {
        var service = CreateService();
        var user = MakeUser();
        var roles = new List<string> { "Admin", "User" };

        var token = service.GenerateAccessToken(user, roles);

        var handler = new JwtSecurityTokenHandler();
        var parsed = handler.ReadJwtToken(token);
        var roleClaims = parsed.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToList();
        roleClaims.Should().BeEquivalentTo(roles);
    }

    [Fact]
    public void GenerateAccessToken_TokenContainsCorrectIssuerAndAudience()
    {
        var service = CreateService();
        var user = MakeUser();

        var token = service.GenerateAccessToken(user, new List<string>());

        var handler = new JwtSecurityTokenHandler();
        var parsed = handler.ReadJwtToken(token);
        parsed.Issuer.Should().Be("test-issuer");
        parsed.Audiences.Should().Contain("test-audience");
    }

    // --- GenerateRefreshToken ---

    [Fact]
    public void GenerateRefreshToken_ReturnsNonEmptyString()
    {
        var service = CreateService();

        var token = service.GenerateRefreshToken();

        token.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void GenerateRefreshToken_ReturnsUniqueTokensEachCall()
    {
        var service = CreateService();

        var token1 = service.GenerateRefreshToken();
        var token2 = service.GenerateRefreshToken();

        token1.Should().NotBe(token2);
    }

    // --- GetPrincipalFromExpiredToken ---

    [Fact]
    public void GetPrincipalFromExpiredToken_WithValidToken_ReturnsPrincipal()
    {
        var service = CreateService(accessTokenMinutes: 60);
        var user = MakeUser();
        var token = service.GenerateAccessToken(user, new List<string>());

        var principal = service.GetPrincipalFromExpiredToken(token);

        principal.Should().NotBeNull();
        principal!.FindFirstValue(ClaimTypes.NameIdentifier).Should().Be(user.Id);
    }

    [Fact]
    public void GetPrincipalFromExpiredToken_WithInvalidToken_ReturnsNull()
    {
        var service = CreateService();

        var principal = service.GetPrincipalFromExpiredToken("not.a.valid.token");

        principal.Should().BeNull();
    }

    [Fact]
    public void GetPrincipalFromExpiredToken_WithTokenFromDifferentSecret_ReturnsNull()
    {
        var service1 = CreateService();
        var service2 = new JwtService(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Secret"] = "completely-different-secret-key-for-testing-purposes",
                ["Jwt:Issuer"] = "test-issuer",
                ["Jwt:Audience"] = "test-audience",
                ["Jwt:AccessTokenExpirationMinutes"] = "60",
                ["Jwt:RefreshTokenExpirationDays"] = "7"
            })
            .Build());
        var user = MakeUser();
        var token = service1.GenerateAccessToken(user, new List<string>());

        var principal = service2.GetPrincipalFromExpiredToken(token);

        principal.Should().BeNull();
    }

    // --- GetAccessTokenExpiration ---

    [Fact]
    public void GetAccessTokenExpiration_ReturnsDateInFuture()
    {
        var service = CreateService(accessTokenMinutes: 60);

        var expiration = service.GetAccessTokenExpiration();

        expiration.Should().BeAfter(DateTime.UtcNow);
    }

    // --- GetRefreshTokenExpiration ---

    [Fact]
    public void GetRefreshTokenExpiration_ReturnsDateInFuture()
    {
        var service = CreateService(refreshTokenDays: 7);

        var expiration = service.GetRefreshTokenExpiration();

        expiration.Should().BeAfter(DateTime.UtcNow);
    }
}
