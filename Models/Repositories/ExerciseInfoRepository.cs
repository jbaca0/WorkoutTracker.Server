using System;
using Microsoft.EntityFrameworkCore;
using WorkoutTracker.Server.DomainObjects;

namespace WorkoutTracker.Server.Models.Repositories;

public class ExerciseInfoRepository
{
    private ApplicationDbContext dbContext;

    public ExerciseInfoRepository(ApplicationDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<List<DomainObjects.ExerciseInfo>> GetByIdsAsync(List<Guid> exerciseInfoIds)
    {
        var modelExerciseInfos = await dbContext.ExerciseInfos
            .Where(e => exerciseInfoIds.Contains(e.Id))
            .ToListAsync();

        return modelExerciseInfos.Select(DomainObjects.ExerciseInfo.FromModel).ToList();
    }

    public async Task<List<DomainObjects.ExerciseInfo>> GetAllAsync()
    {
        var modelExerciseInfos = await dbContext.ExerciseInfos
            .ToListAsync();

        return modelExerciseInfos.Select(DomainObjects.ExerciseInfo.FromModel).ToList();
    }

    public async Task<List<DomainObjects.ExerciseInfo>> GetForUserAsync(Guid userId)
    {
        var modelExerciseInfos = await dbContext.ExerciseInfos
            .Where(e => e.CreatedByUserId == null || e.CreatedByUserId == userId)
            .ToListAsync();

        return modelExerciseInfos.Select(DomainObjects.ExerciseInfo.FromModel).ToList();
    }

    public async Task<DomainObjects.ExerciseInfo?> GetByIdAsync(Guid id)
    {
        var model = await dbContext.ExerciseInfos.FindAsync(id);
        return model != null ? DomainObjects.ExerciseInfo.FromModel(model) : null;
    }

    public async Task<bool> Add(DomainObjects.ExerciseInfo exerciseInfo)
    {
        dbContext.ExerciseInfos.Add(exerciseInfo.ToModel());
        var entriesAdded = await dbContext.SaveChangesAsync();
        return entriesAdded > 0;
    }

    public async Task Update(DomainObjects.ExerciseInfo exerciseInfo)
    {
        var existing = await dbContext.ExerciseInfos.FindAsync(exerciseInfo.Id);
        if (existing == null) return;

        var updated = exerciseInfo.ToModel();
        existing.Name = updated.Name;
        existing.PrimaryMuscleGroup = updated.PrimaryMuscleGroup;
        existing.SecondaryMuscleGroupsJson = updated.SecondaryMuscleGroupsJson;

        await dbContext.SaveChangesAsync();
    }

    public async Task<bool> Delete(Guid id)
    {
        var existing = await dbContext.ExerciseInfos.FindAsync(id);
        if (existing == null) return false;

        dbContext.ExerciseInfos.Remove(existing);
        await dbContext.SaveChangesAsync();
        return true;
    }
}
