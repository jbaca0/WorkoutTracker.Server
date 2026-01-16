using System;
using System.Security.Claims;

namespace WorkoutTracker.Server.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal claims)
    {
        var userId = claims.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId != null && Guid.TryParse(userId, out var userIdGuid))
        {
            return userIdGuid;
        }
        throw new Exception("UserId Not Found");
    }

    public static bool TryGetUserId(this ClaimsPrincipal claims, out Guid userId)
    {
        var userIdString = claims.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userIdString != null && Guid.TryParse(userIdString, out var userIdGuid))
        {
            userId = userIdGuid;
            return true;
        }
        else
        {
            userId = default;
            return false;
        }
    }
}

