using System;

namespace WorkoutTracker.Server.Extensions;

public static class SessionExtensions
{
    private const string UserIdKey = "UserId";

    public static string? GetUserId(this ISession session)
    {
        return session.GetString(UserIdKey);
    }
}
