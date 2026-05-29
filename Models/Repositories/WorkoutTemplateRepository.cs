using Microsoft.EntityFrameworkCore;

namespace WorkoutTracker.Server.Models.Repositories;

public class WorkoutTemplateRepository
{
    private readonly ApplicationDbContext dbContext;

    public WorkoutTemplateRepository(ApplicationDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<List<DomainObjects.WorkoutTemplate>> GetAllForUserAsync(Guid userId)
    {
        var models = await dbContext.WorkoutTemplates
            .Where(t => t.UserId == userId)
            .Include(t => t.Exercises)
                .ThenInclude(e => e.ExerciseInfo)
            .OrderBy(t => t.CreatedDateTime)
            .ToListAsync();

        return models.Select(DomainObjects.WorkoutTemplate.FromModel).ToList();
    }

    public async Task<WorkoutTemplate?> GetByIdAsync(Guid id)
    {
        return await dbContext.WorkoutTemplates
            .Where(t => t.Id == id)
            .Include(t => t.Exercises)
                .ThenInclude(e => e.ExerciseInfo)
            .FirstOrDefaultAsync();
    }

    public async Task<Guid> AddAsync(Guid userId, string name, List<Guid> exerciseInfoIds)
    {
        var template = new WorkoutTemplate
        {
            Id = Guid.NewGuid(),
            Name = name,
            UserId = userId,
            Exercises = exerciseInfoIds.Select((id, index) => new WorkoutTemplateExercise
            {
                Id = Guid.NewGuid(),
                ExerciseInfoId = id,
                Order = index
            }).ToList()
        };

        dbContext.WorkoutTemplates.Add(template);
        await dbContext.SaveChangesAsync();
        return template.Id;
    }

    public async Task<bool> UpdateAsync(Guid templateId, string name, List<Guid> exerciseInfoIds)
    {
        var template = await dbContext.WorkoutTemplates
            .Where(t => t.Id == templateId)
            .Include(t => t.Exercises)
            .FirstOrDefaultAsync();

        if (template == null) return false;

        template.Name = name;
        dbContext.WorkoutTemplateExercises.RemoveRange(template.Exercises);
        template.Exercises = exerciseInfoIds.Select((id, index) => new WorkoutTemplateExercise
        {
            Id = Guid.NewGuid(),
            WorkoutTemplateId = templateId,
            ExerciseInfoId = id,
            Order = index
        }).ToList();

        await dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var template = await dbContext.WorkoutTemplates.FindAsync(id);
        if (template == null) return false;

        dbContext.WorkoutTemplates.Remove(template);
        await dbContext.SaveChangesAsync();
        return true;
    }
}
