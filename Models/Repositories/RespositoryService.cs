using System;

namespace WorkoutTracker.Server.Models.Repositories;

public static class RespositoryService
{
    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<WorkoutRepository>();
        services.AddScoped<ExerciseInfoRepository>();
        services.AddScoped<ExerciseRepository>();
        services.AddScoped<LogEntryRepository>();
        services.AddScoped<WorkoutTemplateRepository>();
        services.AddScoped<WorkoutPlanRepository>();

        return services;
    }
}
