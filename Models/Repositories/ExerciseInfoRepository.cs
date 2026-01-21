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

    public async Task<bool> Add(DomainObjects.ExerciseInfo exerciseInfo)
    {
        dbContext.ExerciseInfos.Add(exerciseInfo.ToModel());
        var entriesAdded = await dbContext.SaveChangesAsync();
        return entriesAdded > 0;
    }
}
