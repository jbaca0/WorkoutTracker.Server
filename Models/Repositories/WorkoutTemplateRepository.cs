using Microsoft.EntityFrameworkCore;
using WorkoutTracker.Server.DataTransferObjects.Requests.WorkoutTemplate;

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
            .Include(t => t.Exercises)
                .ThenInclude(e => e.Sets)
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
            .Include(t => t.Exercises)
                .ThenInclude(e => e.Sets)
            .FirstOrDefaultAsync();
    }

    public async Task<Guid> AddAsync(Guid userId, string name, List<WorkoutTemplateExerciseRequest> exercises)
    {
        var template = new WorkoutTemplate
        {
            Id = Guid.NewGuid(),
            Name = name,
            UserId = userId,
            Exercises = exercises.Select((e, index) =>
            {
                var exerciseId = Guid.NewGuid();
                return new WorkoutTemplateExercise
                {
                    Id = exerciseId,
                    ExerciseInfoId = e.exerciseInfoId,
                    Order = index,
                    Sets = e.sets.Select((s, setIndex) => new WorkoutTemplateExerciseSet
                    {
                        Id = Guid.NewGuid(),
                        WorkoutTemplateExerciseId = exerciseId,
                        Order = setIndex,
                        Reps = s.reps
                    }).ToList()
                };
            }).ToList()
        };

        dbContext.WorkoutTemplates.Add(template);
        await dbContext.SaveChangesAsync();
        return template.Id;
    }

    public async Task<bool> UpdateAsync(Guid templateId, string name, List<WorkoutTemplateExerciseRequest> exercises)
    {
        var template = await dbContext.WorkoutTemplates
            .Where(t => t.Id == templateId)
            .Include(t => t.Exercises)
                .ThenInclude(e => e.Sets)
            .FirstOrDefaultAsync();

        if (template == null) return false;

        template.Name = name;
        dbContext.WorkoutTemplateExercises.RemoveRange(template.Exercises);
        template.Exercises = exercises.Select((e, index) =>
        {
            var exerciseId = Guid.NewGuid();
            return new WorkoutTemplateExercise
            {
                Id = exerciseId,
                WorkoutTemplateId = templateId,
                ExerciseInfoId = e.exerciseInfoId,
                Order = index,
                Sets = e.sets.Select((s, setIndex) => new WorkoutTemplateExerciseSet
                {
                    Id = Guid.NewGuid(),
                    WorkoutTemplateExerciseId = exerciseId,
                    Order = setIndex,
                    Reps = s.reps
                }).ToList()
            };
        }).ToList();

        await dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var template = await dbContext.WorkoutTemplates.FindAsync(id);
        if (template == null) return false;

        // WorkoutPlanDay.WorkoutTemplateId is an optional FK with no cascade, so
        // the delete would be rejected while any plan day still references this
        // template. Clear those days first, leaving the day itself intact.
        var referencingDays = await dbContext.WorkoutPlanDays
            .Where(d => d.WorkoutTemplateId == id)
            .ToListAsync();

        foreach (var day in referencingDays)
        {
            day.WorkoutTemplateId = null;
        }

        dbContext.WorkoutTemplates.Remove(template);
        await dbContext.SaveChangesAsync();
        return true;
    }
}
