using Microsoft.EntityFrameworkCore;
using WorkoutTracker.Server.DataTransferObjects.Requests.WorkoutPlan;

namespace WorkoutTracker.Server.Models.Repositories;

public class WorkoutPlanRepository
{
    private readonly ApplicationDbContext dbContext;

    public WorkoutPlanRepository(ApplicationDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<List<DomainObjects.WorkoutPlan>> GetAllForUserAsync(Guid userId)
    {
        var models = await dbContext.WorkoutPlans
            .Where(p => p.UserId == userId)
            .Include(p => p.Days)
                .ThenInclude(d => d.WorkoutTemplate)
            .OrderBy(p => p.CreatedDateTime)
            .ToListAsync();

        return models.Select(DomainObjects.WorkoutPlan.FromModel).ToList();
    }

    public async Task<WorkoutPlan?> GetByIdAsync(Guid id)
    {
        return await dbContext.WorkoutPlans.FindAsync(id);
    }

    public async Task<DomainObjects.WorkoutPlan?> GetByIdWithDetailsAsync(Guid id)
    {
        var plan = await dbContext.WorkoutPlans
            .Where(p => p.Id == id)
            .Include(p => p.Days)
                .ThenInclude(d => d.WorkoutTemplate)
                    .ThenInclude(t => t!.Exercises)
                        .ThenInclude(e => e.ExerciseInfo)
            .FirstOrDefaultAsync();

        return plan != null ? DomainObjects.WorkoutPlan.FromModel(plan) : null;
    }

    public async Task<Guid> AddAsync(Guid userId, string name, DateTime startDate, DateTime? endDate, List<WorkoutPlanDayRequest> days)
    {
        // Activate automatically when the user has nothing active, so a first plan
        // shows up on the home screen without a separate "Set Active" step.
        var hasActivePlan = await dbContext.WorkoutPlans
            .AnyAsync(p => p.UserId == userId && p.IsActive);

        var plan = new WorkoutPlan
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = name,
            IsActive = !hasActivePlan,
            StartDate = startDate,
            EndDate = endDate,
            Days = days.Select(d => new WorkoutPlanDay
            {
                Id = Guid.NewGuid(),
                DayOfWeek = d.dayOfWeek,
                WorkoutTemplateId = d.workoutTemplateId,
                Note = d.note
            }).ToList()
        };

        dbContext.WorkoutPlans.Add(plan);
        await dbContext.SaveChangesAsync();
        return plan.Id;
    }

    public async Task<bool> UpdateAsync(Guid planId, string name, DateTime startDate, DateTime? endDate, List<WorkoutPlanDayRequest> days)
    {
        var plan = await dbContext.WorkoutPlans
            .Where(p => p.Id == planId)
            .Include(p => p.Days)
            .FirstOrDefaultAsync();

        if (plan == null) return false;

        plan.Name = name;
        plan.StartDate = startDate;
        plan.EndDate = endDate;

        // Materialise before removing; see WorkoutTemplateRepository.UpdateAsync.
        dbContext.WorkoutPlanDays.RemoveRange(plan.Days.ToList());

        var replacements = days.Select(d => new WorkoutPlanDay
        {
            Id = Guid.NewGuid(),
            WorkoutPlanId = planId,
            DayOfWeek = d.dayOfWeek,
            WorkoutTemplateId = d.workoutTemplateId,
            Note = d.note
        }).ToList();

        dbContext.WorkoutPlanDays.AddRange(replacements);

        await dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(Guid planId)
    {
        var plan = await dbContext.WorkoutPlans.FindAsync(planId);
        if (plan == null) return false;

        dbContext.WorkoutPlans.Remove(plan);
        await dbContext.SaveChangesAsync();
        return true;
    }

    public async Task ActivateAsync(Guid userId, Guid planId)
    {
        var userPlans = await dbContext.WorkoutPlans
            .Where(p => p.UserId == userId)
            .ToListAsync();

        foreach (var plan in userPlans)
        {
            plan.IsActive = plan.Id == planId;
        }

        await dbContext.SaveChangesAsync();
    }

    public async Task<DomainObjects.WorkoutPlanDay?> GetTodaysPlanDayAsync(Guid userId, int dayOfWeek)
    {
        var day = await dbContext.WorkoutPlanDays
            .Where(d => d.WorkoutPlan.UserId == userId && d.WorkoutPlan.IsActive && d.DayOfWeek == dayOfWeek)
            .Include(d => d.WorkoutTemplate)
                .ThenInclude(t => t!.Exercises)
                    .ThenInclude(e => e.ExerciseInfo)
            .FirstOrDefaultAsync();

        return day != null ? DomainObjects.WorkoutPlanDay.FromModel(day) : null;
    }
}
