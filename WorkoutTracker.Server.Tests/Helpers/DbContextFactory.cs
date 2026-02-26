using Microsoft.EntityFrameworkCore;
using WorkoutTracker.Server.Models;

namespace WorkoutTracker.Server.Tests.Helpers;

public static class DbContextFactory
{
    public static ApplicationDbContext CreateInMemory(string? dbName = null)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(dbName ?? Guid.NewGuid().ToString())
            .Options;
        var context = new ApplicationDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }
}
