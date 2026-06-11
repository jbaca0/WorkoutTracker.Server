using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WorkoutTracker.Server.DomainObjects;

namespace WorkoutTracker.Server.Models.Repositories;

public class ExerciseRepository
{
    private readonly ApplicationDbContext dbContext;

    public ExerciseRepository(ApplicationDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<List<DomainObjects.ExerciseInfo>> GetExerciseInfosByIdsAsync(List<Guid> exerciseInfoIds)
    {
        var models = await dbContext.ExerciseInfos
            .Where(ei => exerciseInfoIds.Contains(ei.Id))
            .ToListAsync();
        return models.Select(DomainObjects.ExerciseInfo.FromModel).ToList();
    }

    public async Task<List<DomainObjects.Exercise>> GetLatestExercisesForUserAsync(Guid userId, List<Guid> exerciseInfoIds, Guid? excludeWorkoutId = null)
    {
        var latestExerciseIds = await dbContext.Exercises
            .Where(e => exerciseInfoIds.Contains(e.ExerciseInfoId))
            .Where(e => e.Workout.UserId == userId)
            .Where(e => excludeWorkoutId == null || e.WorkoutId != excludeWorkoutId)
            .GroupBy(e => e.ExerciseInfoId)
            .Select(g => g.OrderByDescending(e => e.Workout.AddDateTime).First().Id)
            .ToListAsync();

        var latestExercises = await dbContext.Exercises
            .Include(e => e.ExerciseInfo)
            .Include(e => e.Sets)
            .Where(e => latestExerciseIds.Contains(e.Id))
            .ToListAsync();

        return latestExercises.Select(DomainObjects.Exercise.FromModel).ToList();
    }

    public async Task<(decimal weight, int reps)?> GetBestSetByEpleyAsync(Guid userId, Guid exerciseInfoId)
    {
        var sets = await dbContext.Sets
            .Where(s => s.Exercise.ExerciseInfoId == exerciseInfoId)
            .Where(s => s.Exercise.Workout.UserId == userId)
            .Select(s => new { s.Weight, s.Reps })
            .ToListAsync();

        if (sets.Count == 0) return null;

        var best = sets
            .OrderByDescending(s => s.Weight * (1 + s.Reps / 30m))
            .First();

        return (best.Weight, best.Reps);
    }
}
