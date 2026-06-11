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
        // Remove existing exercises and their sets
        var existingExercises = await dbContext.Exercises
            .Where(e => e.WorkoutId == workoutId)
            .Include(e => e.Sets)
            .ToListAsync();

        dbContext.Exercises.RemoveRange(existingExercises);

        // Add new exercises with properly linked sets
        foreach (var exercise in exercises)
        {
            var modelExercise = new Exercise
            {
                Id = Guid.NewGuid(),
                WorkoutId = workoutId,
                ExerciseInfoId = exercise.ExerciseInfoId
            };

            var setOrder = 1;
            foreach (var set in exercise.Sets)
            {
                modelExercise.Sets.Add(new Set
                {
                    Id = Guid.NewGuid(),
                    ExerciseId = modelExercise.Id,
                    Reps = set.Reps,
                    Weight = set.Weight,
                    Completed = set.Completed,
                    Order = setOrder++
                });
            }

            dbContext.Exercises.Add(modelExercise);
        }
    }

    public async Task<List<DomainObjects.Workout>> GetWorkoutsWithSummaryAsync(Guid userId)
    {
        var modelWorkouts = await dbContext.Workouts
            .Where(w => w.UserId == userId)
            .Include(w => w.Exercises)
                .ThenInclude(e => e.ExerciseInfo)
            .OrderByDescending(w => w.AddDateTime)
            .ToListAsync();

        return modelWorkouts.Select(DomainObjects.Workout.FromModel).ToList();
    }

    public async Task<DomainObjects.Workout?> GetWorkoutWithDetailsAsync(Guid workoutId)
    {
        var modelWorkout = await dbContext.Workouts
            .Where(w => w.Id == workoutId)
            .Include(w => w.Exercises)
                .ThenInclude(e => e.ExerciseInfo)
            .Include(w => w.Exercises)
                .ThenInclude(e => e.Sets)
            .FirstOrDefaultAsync();

        if (modelWorkout == null)
        {
            return null;
        }

        return DomainObjects.Workout.FromModel(modelWorkout);
    }

    public async Task<bool> DeleteAsync(Guid workoutId)
    {
        try
        {
            var workout = await dbContext.Workouts
                .Include(w => w.Exercises)
                    .ThenInclude(e => e.Sets)
                .FirstOrDefaultAsync(w => w.Id == workoutId);

            if (workout == null) return false;

            dbContext.Workouts.Remove(workout);
            await dbContext.SaveChangesAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

}
