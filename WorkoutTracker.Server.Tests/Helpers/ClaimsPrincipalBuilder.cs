using System.Security.Claims;

namespace WorkoutTracker.Server.Tests.Helpers;

public static class ClaimsPrincipalBuilder
{
    public static ClaimsPrincipal WithUserId(Guid userId)
    {
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
    }

    public static ClaimsPrincipal WithNoUserId()
    {
        return new ClaimsPrincipal(new ClaimsIdentity(Array.Empty<Claim>(), "Test"));
    }

    public static ClaimsPrincipal WithInvalidUserId()
    {
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, "not-a-guid") };
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
    }
}
