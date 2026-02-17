using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WorkoutTracker.Server.DataTransferObjects.Requests;
using WorkoutTracker.Server.DataTransferObjects.Responses;
using WorkoutTracker.Server.Extensions;
using WorkoutTracker.Server.Models;
using WorkoutTracker.Server.Services;

namespace WorkoutTracker.Server.Handlers;

public static class AuthHandler
{
    public static async Task<IResult> Login(
        LoginRequest request,
        UserManager<IdentityUser> userManager,
        JwtService jwtService,
        ApplicationDbContext dbContext)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            return Results.Unauthorized();
        }

        var passwordValid = await userManager.CheckPasswordAsync(user, request.Password);
        if (!passwordValid)
        {
            return Results.Unauthorized();
        }

        var roles = await userManager.GetRolesAsync(user);
        var accessToken = jwtService.GenerateAccessToken(user, roles);
        var refreshToken = jwtService.GenerateRefreshToken();

        var refreshTokenEntity = new RefreshToken
        {
            Token = refreshToken,
            UserId = user.Id,
            ExpiresAt = jwtService.GetRefreshTokenExpiration(),
            IsRevoked = false
        };

        dbContext.RefreshTokens.Add(refreshTokenEntity);
        await dbContext.SaveChangesAsync();

        return Results.Ok(new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = jwtService.GetAccessTokenExpiration(),
            Roles = roles.ToList()
        });
    }

    public static async Task<IResult> Refresh(
        RefreshTokenRequest request,
        JwtService jwtService,
        UserManager<IdentityUser> userManager,
        ApplicationDbContext dbContext)
    {
        var storedToken = await dbContext.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken);

        if (storedToken == null || storedToken.IsRevoked || storedToken.ExpiresAt < DateTime.UtcNow)
        {
            return Results.Unauthorized();
        }

        var user = await userManager.FindByIdAsync(storedToken.UserId);
        if (user == null)
        {
            return Results.Unauthorized();
        }

        // Revoke old refresh token
        storedToken.IsRevoked = true;

        // Generate new tokens
        var roles = await userManager.GetRolesAsync(user);
        var newAccessToken = jwtService.GenerateAccessToken(user, roles);
        var newRefreshToken = jwtService.GenerateRefreshToken();

        var newRefreshTokenEntity = new RefreshToken
        {
            Token = newRefreshToken,
            UserId = user.Id,
            ExpiresAt = jwtService.GetRefreshTokenExpiration(),
            IsRevoked = false
        };

        dbContext.RefreshTokens.Add(newRefreshTokenEntity);
        await dbContext.SaveChangesAsync();

        return Results.Ok(new AuthResponse
        {
            AccessToken = newAccessToken,
            RefreshToken = newRefreshToken,
            ExpiresAt = jwtService.GetAccessTokenExpiration(),
            Roles = roles.ToList()
        });
    }

    public static async Task<IResult> Logout(
        RefreshTokenRequest request,
        ClaimsPrincipal user,
        ApplicationDbContext dbContext)
    {
        var userId = user.GetUserId().ToString();

        var storedToken = await dbContext.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken && rt.UserId == userId);

        if (storedToken != null)
        {
            storedToken.IsRevoked = true;
            await dbContext.SaveChangesAsync();
        }

        return Results.Ok();
    }
}
