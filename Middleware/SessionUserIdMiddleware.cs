using System;
using Microsoft.AspNetCore.Identity;

namespace WorkoutTracker.Server.Middleware;

public class SessionUserIdMiddleware
{
    private readonly RequestDelegate next;

    public SessionUserIdMiddleware(RequestDelegate next)
    {
        this.next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.User.Identity != null
            && context.User.Identity.IsAuthenticated)
        {
            var userManager = context.RequestServices.GetRequiredService<UserManager<IdentityUser>>();
            var user = await userManager.GetUserAsync(context.User);
            if (user != null)
            {
                context.Session.SetString("UserId", user.Id);
            }
        }
        await this.next(context);
    }
}

public static class SessionUserIdMiddlewareExtensions
{
    public static IApplicationBuilder UseSessionUserId(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<SessionUserIdMiddleware>();
    }
}
