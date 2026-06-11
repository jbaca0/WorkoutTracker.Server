using FluentAssertions;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using WorkoutTracker.Server.DataTransferObjects.Requests;
using WorkoutTracker.Server.DataTransferObjects.Responses;
using WorkoutTracker.Server.Handlers;
using WorkoutTracker.Server.Models;
using WorkoutTracker.Server.Services;
using WorkoutTracker.Server.Tests.Helpers;
using Xunit;

namespace WorkoutTracker.Server.Tests.Handlers;

public class AuthHandlerTests
{
    private const string Email = "user@example.com";
    private const string Password = "Password1!";

    private static JwtService CreateJwtService()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Secret"] = "super-secret-key-that-is-long-enough-for-hmac-sha256",
                ["Jwt:Issuer"] = "test-issuer",
                ["Jwt:Audience"] = "test-audience",
                ["Jwt:AccessTokenExpirationMinutes"] = "60",
                ["Jwt:RefreshTokenExpirationDays"] = "7"
            })
            .Build();
        return new JwtService(config);
    }

    private static async Task<IdentityUser> CreateUserAsync(
        UserManager<IdentityUser> userManager,
        string email = Email,
        string password = Password)
    {
        var user = new IdentityUser { UserName = email, Email = email };
        var result = await userManager.CreateAsync(user, password);
        result.Succeeded.Should().BeTrue();
        return user;
    }

    // --- Login ---

    [Fact]
    public async Task Login_WithUnknownEmail_ReturnsUnauthorized()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var userManager = IdentityFactory.CreateUserManager(dbContext);
        var request = new LoginRequest { Email = "nobody@example.com", Password = Password };

        var result = await AuthHandler.Login(request, userManager, CreateJwtService(), dbContext);

        result.Should().BeOfType<UnauthorizedHttpResult>();
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsUnauthorized()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var userManager = IdentityFactory.CreateUserManager(dbContext);
        await CreateUserAsync(userManager);
        var request = new LoginRequest { Email = Email, Password = "WrongPassword1!" };

        var result = await AuthHandler.Login(request, userManager, CreateJwtService(), dbContext);

        result.Should().BeOfType<UnauthorizedHttpResult>();
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsTokensAndPersistsRefreshToken()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var userManager = IdentityFactory.CreateUserManager(dbContext);
        var user = await CreateUserAsync(userManager);
        var request = new LoginRequest { Email = Email, Password = Password };

        var result = await AuthHandler.Login(request, userManager, CreateJwtService(), dbContext);

        var ok = result.Should().BeOfType<Ok<AuthResponse>>().Subject;
        ok.Value!.AccessToken.Should().NotBeNullOrEmpty();
        ok.Value.RefreshToken.Should().NotBeNullOrEmpty();
        ok.Value.ExpiresAt.Should().BeAfter(DateTime.UtcNow);

        var storedToken = dbContext.RefreshTokens.Should().ContainSingle().Subject;
        storedToken.Token.Should().Be(ok.Value.RefreshToken);
        storedToken.UserId.Should().Be(user.Id);
        storedToken.IsRevoked.Should().BeFalse();
    }

    // --- Refresh ---

    [Fact]
    public async Task Refresh_WithUnknownToken_ReturnsUnauthorized()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var userManager = IdentityFactory.CreateUserManager(dbContext);
        var request = new RefreshTokenRequest { RefreshToken = "does-not-exist" };

        var result = await AuthHandler.Refresh(request, CreateJwtService(), userManager, dbContext);

        result.Should().BeOfType<UnauthorizedHttpResult>();
    }

    [Fact]
    public async Task Refresh_WithRevokedToken_ReturnsUnauthorized()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var userManager = IdentityFactory.CreateUserManager(dbContext);
        var user = await CreateUserAsync(userManager);
        dbContext.RefreshTokens.Add(new RefreshToken
        {
            Token = "revoked-token",
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsRevoked = true
        });
        await dbContext.SaveChangesAsync();

        var request = new RefreshTokenRequest { RefreshToken = "revoked-token" };
        var result = await AuthHandler.Refresh(request, CreateJwtService(), userManager, dbContext);

        result.Should().BeOfType<UnauthorizedHttpResult>();
    }

    [Fact]
    public async Task Refresh_WithExpiredToken_ReturnsUnauthorized()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var userManager = IdentityFactory.CreateUserManager(dbContext);
        var user = await CreateUserAsync(userManager);
        dbContext.RefreshTokens.Add(new RefreshToken
        {
            Token = "expired-token",
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddMinutes(-1),
            IsRevoked = false
        });
        await dbContext.SaveChangesAsync();

        var request = new RefreshTokenRequest { RefreshToken = "expired-token" };
        var result = await AuthHandler.Refresh(request, CreateJwtService(), userManager, dbContext);

        result.Should().BeOfType<UnauthorizedHttpResult>();
    }

    [Fact]
    public async Task Refresh_WhenUserNoLongerExists_ReturnsUnauthorized()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var userManager = IdentityFactory.CreateUserManager(dbContext);
        dbContext.RefreshTokens.Add(new RefreshToken
        {
            Token = "orphan-token",
            UserId = Guid.NewGuid().ToString(),
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsRevoked = false
        });
        await dbContext.SaveChangesAsync();

        var request = new RefreshTokenRequest { RefreshToken = "orphan-token" };
        var result = await AuthHandler.Refresh(request, CreateJwtService(), userManager, dbContext);

        result.Should().BeOfType<UnauthorizedHttpResult>();
    }

    [Fact]
    public async Task Refresh_WithValidToken_RotatesTokensAndRevokesOldOne()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var userManager = IdentityFactory.CreateUserManager(dbContext);
        var user = await CreateUserAsync(userManager);
        var oldToken = new RefreshToken
        {
            Token = "valid-token",
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsRevoked = false
        };
        dbContext.RefreshTokens.Add(oldToken);
        await dbContext.SaveChangesAsync();

        var request = new RefreshTokenRequest { RefreshToken = "valid-token" };
        var result = await AuthHandler.Refresh(request, CreateJwtService(), userManager, dbContext);

        var ok = result.Should().BeOfType<Ok<AuthResponse>>().Subject;
        ok.Value!.AccessToken.Should().NotBeNullOrEmpty();
        ok.Value.RefreshToken.Should().NotBe("valid-token");

        oldToken.IsRevoked.Should().BeTrue();
        dbContext.RefreshTokens.Should().HaveCount(2);
        dbContext.RefreshTokens.Should().ContainSingle(rt => rt.Token == ok.Value.RefreshToken && !rt.IsRevoked);
    }

    // --- Logout ---

    [Fact]
    public async Task Logout_WithOwnToken_RevokesIt()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var userId = Guid.NewGuid();
        var token = new RefreshToken
        {
            Token = "my-token",
            UserId = userId.ToString(),
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsRevoked = false
        };
        dbContext.RefreshTokens.Add(token);
        await dbContext.SaveChangesAsync();

        var request = new RefreshTokenRequest { RefreshToken = "my-token" };
        var result = await AuthHandler.Logout(request, ClaimsPrincipalBuilder.WithUserId(userId), dbContext);

        result.Should().BeOfType<Ok>();
        token.IsRevoked.Should().BeTrue();
    }

    [Fact]
    public async Task Logout_WithAnotherUsersToken_DoesNotRevokeIt()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var otherUsersToken = new RefreshToken
        {
            Token = "other-token",
            UserId = Guid.NewGuid().ToString(),
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsRevoked = false
        };
        dbContext.RefreshTokens.Add(otherUsersToken);
        await dbContext.SaveChangesAsync();

        var request = new RefreshTokenRequest { RefreshToken = "other-token" };
        var result = await AuthHandler.Logout(request, ClaimsPrincipalBuilder.WithUserId(Guid.NewGuid()), dbContext);

        result.Should().BeOfType<Ok>();
        otherUsersToken.IsRevoked.Should().BeFalse();
    }

    [Fact]
    public async Task Logout_WithUnknownToken_StillReturnsOk()
    {
        var dbContext = DbContextFactory.CreateInMemory();

        var request = new RefreshTokenRequest { RefreshToken = "unknown-token" };
        var result = await AuthHandler.Logout(request, ClaimsPrincipalBuilder.WithUserId(Guid.NewGuid()), dbContext);

        result.Should().BeOfType<Ok>();
    }
}
