using System;
using Microsoft.EntityFrameworkCore;


namespace WorkoutTracker.Server.Models.Repositories;

public class WorkoutRepository
{
    private ApplicationDbContext dbContext;

    public WorkoutRepository(ApplicationDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<Workout?> GetByIdAsync(Guid workoutId)
    {
        return await dbContext.Workouts.FindAsync(workoutId);
    }

    public async Task<List<DomainObjects.Exercise>> GetExercisesAsync(Guid workoutId)
    {
        var modelExercises = await dbContext.Exercises
            .Where(e => e.WorkoutId == workoutId)
            .Include(e => e.ExerciseInfo)
            .ToListAsync();

        return modelExercises.Select(DomainObjects.Exercise.FromModel).ToList();
    }

    public async Task<bool> AddExercises(Guid workoutId, IEnumerable<Guid> exerciseInfoIds)
    {
        var workout = await GetByIdAsync(workoutId);
        if (workout == null)
        {
            return false;
        }

        var exercises = exerciseInfoIds.Select(id => new Exercise
        {
            WorkoutId = workoutId,
            ExerciseInfoId = id
        });
        dbContext.Exercises.AddRange(exercises);
        await dbContext.SaveChangesAsync();

        return true;
    }

    public async Task<bool> SaveAsync(DomainObjects.Workout workout)
    {
        try
        {
            // Check if the workout already exists
            var existingWorkout = await dbContext.Workouts.FindAsync(workout.Id);

            if (existingWorkout == null)
            {
                // Add new workout
                dbContext.Workouts.Add(workout.ToModel());
            }
            else
            {
                // Only update exercises, not the workout properties
                await UpdateExercisesAsync(workout.Id, workout.Exercises);
            }

            await dbContext.SaveChangesAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    private async Task UpdateExercisesAsync(Guid workoutId, List<DomainObjects.Exercise> exercises)
    {
        // Remove existing exercises
        var existingExercises = await dbContext.Exercises
            .Where(e => e.WorkoutId == workoutId)
            .ToListAsync();

        dbContext.Exercises.RemoveRange(existingExercises);

        // Add new exercises
        var modelExercises = exercises.Select(e => e.ToModel()).ToList();
        dbContext.Exercises.AddRange(modelExercises);
    }

}
