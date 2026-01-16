using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using WorkoutTracker.Server.Authorization;
using WorkoutTracker.Server.Extensions;
using WorkoutTracker.Server.Middleware;
using WorkoutTracker.Server.Models;
using WorkoutTracker.Server.Models.Repositories;
using WorkoutTracker.Server.Routes;
using WorkoutTracker.Server.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddDbContext<ApplicationDbContext>();
builder.Services.AddRepositories();

builder.Services.AddAuthorization();
builder.Services.AddResourceAuthorization();

builder.Services.AddScoped<UserExerciseInfoService>();

builder.Services.Configure<IdentityOptions>(options =>
{
    // Password settings (relaxed)
    options.Password.RequireDigit = false;
    options.Password.RequiredLength = 3;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
});

builder.Services.AddIdentityApiEndpoints<IdentityUser>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(builder =>
    {
        builder.WithOrigins("http://localhost:5173", "capacitor://localhost")
               .AllowAnyMethod()
               .AllowAnyHeader()
               .AllowCredentials();
    });
});

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors();
app.UseSession();
app.UseSessionUserId();

app.MapIdentityApi<IdentityUser>();

app.MapPost("/logout", async (SignInManager<IdentityUser> signInManager) =>
{
    await signInManager.SignOutAsync();
    return Results.Ok();
})
.WithOpenApi()
.RequireAuthorization();

app.MapWorkoutRoutes();
app.MapExerciseInfoRoutes();

app.Run();

