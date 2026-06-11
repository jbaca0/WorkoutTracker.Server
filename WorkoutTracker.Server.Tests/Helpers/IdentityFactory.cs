using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using WorkoutTracker.Server.Models;

namespace WorkoutTracker.Server.Tests.Helpers;

/// <summary>
/// Creates real UserManager/RoleManager instances backed by an InMemory
/// ApplicationDbContext, matching the convention of testing against real
/// repositories rather than mocking concrete classes.
/// </summary>
public static class IdentityFactory
{
    public static UserManager<IdentityUser> CreateUserManager(ApplicationDbContext dbContext)
    {
        var store = new UserStore<IdentityUser>(dbContext);
        return new UserManager<IdentityUser>(
            store,
            Options.Create(new IdentityOptions()),
            new PasswordHasher<IdentityUser>(),
            new List<IUserValidator<IdentityUser>> { new UserValidator<IdentityUser>() },
            new List<IPasswordValidator<IdentityUser>> { new PasswordValidator<IdentityUser>() },
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            null!,
            NullLogger<UserManager<IdentityUser>>.Instance);
    }

    public static RoleManager<IdentityRole> CreateRoleManager(ApplicationDbContext dbContext)
    {
        var store = new RoleStore<IdentityRole>(dbContext);
        return new RoleManager<IdentityRole>(
            store,
            new List<IRoleValidator<IdentityRole>> { new RoleValidator<IdentityRole>() },
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            NullLogger<RoleManager<IdentityRole>>.Instance);
    }
}
