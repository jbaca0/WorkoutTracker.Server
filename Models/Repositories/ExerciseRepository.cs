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
}
