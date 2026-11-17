using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WorkoutTracker.Server.Models;

namespace WorkoutTracker.Server.Services;

public static class DatabaseInitializer
{
    public static async Task InitializeDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();

        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await context.Database.MigrateAsync();

        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        if (!await roleManager.RoleExistsAsync("Admin"))
        {
            await roleManager.CreateAsync(new IdentityRole("Admin"));
        }

        // Admin credentials come from configuration only; there is no committed default.
        // In production these are App Service settings (AdminSeed__Email / AdminSeed__Password),
        // locally they belong in the gitignored appsettings.Development.local.json.
        var adminEmail = app.Configuration["AdminSeed:Email"];
        var adminPassword = app.Configuration["AdminSeed:Password"];
        if (!string.IsNullOrEmpty(adminEmail) && !string.IsNullOrEmpty(adminPassword))
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser == null)
            {
                adminUser = new IdentityUser { UserName = adminEmail, Email = adminEmail };
                var result = await userManager.CreateAsync(adminUser, adminPassword);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
                else
                {
                    app.Logger.LogWarning("Admin seed user creation failed: {Errors}",
                        string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }
            else
            {
                if (!await userManager.IsInRoleAsync(adminUser, "Admin"))
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }

                // Keep the configured password authoritative. Without this, changing
                // AdminSeed:Password has no effect on an account that already exists,
                // and there is no password-reset endpoint to fall back on.
                // RemovePassword/AddPassword rather than ResetPasswordAsync: the latter
                // needs a token provider, which AddIdentity does not register.
                if (!await userManager.CheckPasswordAsync(adminUser, adminPassword))
                {
                    var removed = await userManager.RemovePasswordAsync(adminUser);
                    if (removed.Succeeded)
                    {
                        var added = await userManager.AddPasswordAsync(adminUser, adminPassword);
                        if (added.Succeeded)
                        {
                            app.Logger.LogInformation("Admin seed password for {Email} updated from configuration", adminEmail);
                        }
                        else
                        {
                            app.Logger.LogWarning("Admin seed password update failed: {Errors}",
                                string.Join(", ", added.Errors.Select(e => e.Description)));
                        }
                    }
                    else
                    {
                        app.Logger.LogWarning("Admin seed password removal failed: {Errors}",
                            string.Join(", ", removed.Errors.Select(e => e.Description)));
                    }
                }
            }
        }
        else
        {
            app.Logger.LogInformation(
                "AdminSeed:Email/Password not configured; no admin account was seeded.");
        }

        if (app.Environment.IsDevelopment())
        {
            await DevDataSeeder.SeedDevUserAsync(scope.ServiceProvider, app.Configuration, app.Logger);
        }
    }
}
