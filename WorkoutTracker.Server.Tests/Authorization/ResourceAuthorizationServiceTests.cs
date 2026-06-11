using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using WorkoutTracker.Server.Authorization;
using WorkoutTracker.Server.Authorization.Handlers;
using WorkoutTracker.Server.Authorization.Requirements;
using Xunit;

namespace WorkoutTracker.Server.Tests.Authorization;

public class ResourceAuthorizationServiceTests
{
    private static AuthorizationOptions BuildOptions(IServiceCollection services)
    {
        return services.BuildServiceProvider()
            .GetRequiredService<IOptions<AuthorizationOptions>>().Value;
    }

    [Fact]
    public void AddResourceAuthorization_RegistersWorkoutOwnerHandler()
    {
        var services = new ServiceCollection();

        services.AddResourceAuthorization();

        services.Should().Contain(d =>
            d.ServiceType == typeof(IAuthorizationHandler) &&
            d.ImplementationType == typeof(WorkoutOwnerHandler) &&
            d.Lifetime == ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddResourceAuthorization_AddsWorkoutOwnerPolicyWithRequirement()
    {
        var services = new ServiceCollection();
        services.AddResourceAuthorization();

        var options = BuildOptions(services);

        var policy = options.GetPolicy("WorkoutOwner");
        policy.Should().NotBeNull();
        policy!.Requirements.Should().ContainSingle(r => r is WorkoutOwnerRequirement);
    }

    [Fact]
    public void AddResourceAuthorization_AddsAdminOnlyPolicyRequiringAdminRole()
    {
        var services = new ServiceCollection();
        services.AddResourceAuthorization();

        var options = BuildOptions(services);

        var policy = options.GetPolicy("AdminOnly");
        policy.Should().NotBeNull();
        var roleRequirement = policy!.Requirements.OfType<RolesAuthorizationRequirement>().Single();
        roleRequirement.AllowedRoles.Should().Contain("Admin");
    }
}
