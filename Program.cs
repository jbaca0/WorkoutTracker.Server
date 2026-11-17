using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using WorkoutTracker.Server.Authorization;
using WorkoutTracker.Server.Logging;
using WorkoutTracker.Server.Middleware;
using WorkoutTracker.Server.Models;
using WorkoutTracker.Server.Models.Repositories;
using WorkoutTracker.Server.Routes;
using WorkoutTracker.Server.Services;

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsDevelopment())
{
    // Gitignored, machine-local dev settings (e.g. DevSeed credentials).
    builder.Configuration.AddJsonFile("appsettings.Development.local.json", optional: true, reloadOnChange: true);
}

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddRepositories();

builder.Services.AddAuthorization();
builder.Services.AddResourceAuthorization();

builder.Services.AddScoped<UserExerciseInfoService>();
builder.Services.AddScoped<JwtService>();
builder.Services.AddHostedService<RefreshTokenCleanupService>();

builder.Services.Configure<IdentityOptions>(options =>
{
    // Password settings (relaxed)
    options.Password.RequireDigit = false;
    options.Password.RequiredLength = 3;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
});

builder.Services.AddIdentity<IdentityUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

// Configure JWT Authentication
// No signing key is committed. It comes from App Service configuration (Jwt__Secret)
// in production and from the gitignored appsettings.Development.local.json locally.
// Failing fast is deliberate: a missing key must not silently fall back to a known value.
var jwtSecret = builder.Configuration["Jwt:Secret"];
if (string.IsNullOrWhiteSpace(jwtSecret) || Encoding.UTF8.GetByteCount(jwtSecret) < 32)
{
    throw new InvalidOperationException(
        "Jwt:Secret is missing or shorter than 32 bytes. Set it via the Jwt__Secret environment " +
        "variable (App Service configuration) or in appsettings.Development.local.json.");
}

var jwtIssuer = builder.Configuration["Jwt:Issuer"]!;
var jwtAudience = builder.Configuration["Jwt:Audience"]!;

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
    };
});

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(builder =>
    {
        builder.WithOrigins(
            "https://localhost:5173",
            "https://localhost:5174",
            "capacitor://localhost",
            "https://victorious-smoke-09e74db1e.1.azurestaticapps.net"
        )
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

var app = builder.Build();

await app.InitializeDatabaseAsync();

app.Services.GetRequiredService<ILoggerFactory>()
    .AddProvider(new DatabaseLoggerProvider(app.Services, app.Configuration));

app.UseMiddleware<RequestLoggingMiddleware>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors();
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapMonitoringRoutes();
app.MapAuthRoutes();
app.MapWorkoutRoutes();
app.MapExerciseInfoRoutes();
app.MapAdminRoutes();
app.MapWorkoutTemplateRoutes();
app.MapWorkoutPlanRoutes();
app.MapExerciseGoalRoutes();

app.Run();
