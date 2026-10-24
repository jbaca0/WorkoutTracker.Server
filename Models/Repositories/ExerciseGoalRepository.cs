using Microsoft.EntityFrameworkCore;

namespace WorkoutTracker.Server.Models.Repositories;

public class ExerciseGoalRepository
{
    private readonly ApplicationDbContext dbContext;

    public ExerciseGoalRepository(ApplicationDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<List<DomainObjects.ExerciseGoal>> GetByUserIdAsync(Guid userId)
    {
        var models = await dbContext.ExerciseGoals
            .Where(g => g.UserId == userId)
            .Include(g => g.ExerciseInfo)
            .OrderBy(g => g.CreatedAt)
            .ToListAsync();

        return models.Select(DomainObjects.ExerciseGoal.FromModel).ToList();
    }

    public async Task<DomainObjects.ExerciseGoal?> GetByExerciseInfoIdAsync(Guid userId, Guid exerciseInfoId)
    {
        var model = await dbContext.ExerciseGoals
            .Where(g => g.UserId == userId && g.ExerciseInfoId == exerciseInfoId)
            .Include(g => g.ExerciseInfo)
            .FirstOrDefaultAsync();

        return model != null ? DomainObjects.ExerciseGoal.FromModel(model) : null;
    }

    public async Task<List<DomainObjects.ExerciseGoal>> GetByExerciseInfoIdsAsync(Guid userId, List<Guid> exerciseInfoIds)
    {
        var models = await dbContext.ExerciseGoals
            .Where(g => g.UserId == userId && exerciseInfoIds.Contains(g.ExerciseInfoId))
            .Include(g => g.ExerciseInfo)
            .ToListAsync();

        return models.Select(DomainObjects.ExerciseGoal.FromModel).ToList();
    }

    public async Task<List<DomainObjects.ExerciseGoal>> GetByPlanIdAsync(Guid userId, Guid planId)
    {
        var models = await dbContext.ExerciseGoals
            .Where(g => g.UserId == userId && g.WorkoutPlanId == planId)
            .Include(g => g.ExerciseInfo)
            .OrderBy(g => g.CreatedAt)
            .ToListAsync();

        return models.Select(DomainObjects.ExerciseGoal.FromModel).ToList();
    }

    public async Task<ExerciseGoal?> GetModelByIdAsync(Guid id)
    {
        return await dbContext.ExerciseGoals.FindAsync(id);
    }

    public async Task<DomainObjects.ExerciseGoal> CreateAsync(
        Guid userId,
        Guid exerciseInfoId,
        Guid? workoutPlanId,
        decimal startWeight,
        int startReps,
        decimal targetWeight,
        int targetReps,
        DateTime goalDate)
    {
        var existing = await dbContext.ExerciseGoals
            .FirstOrDefaultAsync(g => g.UserId == userId && g.ExerciseInfoId == exerciseInfoId);

        if (existing != null)
        {
            dbContext.ExerciseGoals.Remove(existing);
        }

        var goal = new ExerciseGoal
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ExerciseInfoId = exerciseInfoId,
            WorkoutPlanId = workoutPlanId,
            StartWeight = startWeight,
            StartReps = startReps,
            TargetWeight = targetWeight,
            TargetReps = targetReps,
            GoalDate = goalDate
        };

        dbContext.ExerciseGoals.Add(goal);
        await dbContext.SaveChangesAsync();

        var saved = await dbContext.ExerciseGoals
            .Include(g => g.ExerciseInfo)
            .FirstAsync(g => g.Id == goal.Id);

        return DomainObjects.ExerciseGoal.FromModel(saved);
    }

    public async Task<bool> UpdateAsync(
        Guid id,
        Guid userId,
        decimal startWeight,
        int startReps,
        decimal targetWeight,
        int targetReps,
        DateTime goalDate)
    {
        var goal = await dbContext.ExerciseGoals
            .FirstOrDefaultAsync(g => g.Id == id && g.UserId == userId);

        if (goal == null) return false;

        goal.StartWeight = startWeight;
        goal.StartReps = startReps;
        goal.TargetWeight = targetWeight;
        goal.TargetReps = targetReps;
        goal.GoalDate = goalDate;

        await dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<bool> AssignToPlanAsync(Guid id, Guid userId, Guid? workoutPlanId)
    {
        var goal = await dbContext.ExerciseGoals
            .FirstOrDefaultAsync(g => g.Id == id && g.UserId == userId);

        if (goal == null) return false;

        goal.WorkoutPlanId = workoutPlanId;
        await dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(Guid id, Guid userId)
    {
        var goal = await dbContext.ExerciseGoals
            .FirstOrDefaultAsync(g => g.Id == id && g.UserId == userId);

        if (goal == null) return false;

        dbContext.ExerciseGoals.Remove(goal);
        await dbContext.SaveChangesAsync();
        return true;
    }
}
