using System;
using System.Security.Claims;

namespace WorkoutTracker.Server.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static Guid? GetUserId(this ClaimsPrincipal claims)
    {
        return Guid.TryParse(claims.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)
            ? userId
            : null;
    }
}

