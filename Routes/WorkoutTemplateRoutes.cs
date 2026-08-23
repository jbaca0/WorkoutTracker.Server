using WorkoutTracker.Server.Handlers;

namespace WorkoutTracker.Server.Routes;

public static class WorkoutTemplateRoutes
{
    public static void MapWorkoutTemplateRoutes(this WebApplication app)
    {
        app.MapGet("/getTemplates", WorkoutTemplateHandler.GetTemplates)
            .RequireAuthorization();

        app.MapPost("/addTemplate", WorkoutTemplateHandler.AddTemplate)
            .RequireAuthorization();

        app.MapPut("/updateTemplate", WorkoutTemplateHandler.UpdateTemplate)
            .RequireAuthorization();

        app.MapDelete("/deleteTemplate/{templateId}", WorkoutTemplateHandler.DeleteTemplate)
            .RequireAuthorization();

        app.MapPost("/startWorkoutFromTemplate", WorkoutTemplateHandler.StartWorkoutFromTemplate)
            .RequireAuthorization();
    }
}
