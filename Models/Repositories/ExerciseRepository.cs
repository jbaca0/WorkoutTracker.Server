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

    public async Task<List<DomainObjects.Exercise>> GetLatestExercisesForUserAsync(Guid userId, List<Guid> exerciseInfoIds)
    {
        var latestExercises = await dbContext.Exercises
            .Include(e => e.Workout)
            .Include(e => e.ExerciseInfo)
            .Include(e => e.Sets)
            .Where(e => exerciseInfoIds.Contains(e.ExerciseInfoId))
            .Where(e => e.Workout.UserId == userId)
            .GroupBy(e => e.ExerciseInfoId)
            .Select(g => g.OrderByDescending(e => e.Workout.AddDateTime).First())
            .ToListAsync();

        return latestExercises.Select(DomainObjects.Exercise.FromModel).ToList();
    }
}
