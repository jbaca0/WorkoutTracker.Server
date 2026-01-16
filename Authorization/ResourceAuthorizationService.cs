using System;
using Microsoft.AspNetCore.Authorization;
using WorkoutTracker.Server.Authorization.Handlers;
using WorkoutTracker.Server.Authorization.Requirements;

namespace WorkoutTracker.Server.Authorization;

public static class ResourceAuthorizationService
{
    public static IServiceCollection AddResourceAuthorization(this IServiceCollection services)
    {
        services.AddScoped<IAuthorizationHandler, WorkoutOwnerHandler>();

        services.AddAuthorization(options =>
        {
            options.AddPolicy("WorkoutOwner", policy =>
                policy.Requirements.Add(new WorkoutOwnerRequirement()));
        });


        return services;
    }
}
