using System;
using WorkoutTracker.Server.DomainObjects;
using WorkoutTracker.Server.Models.Repositories;

namespace WorkoutTracker.Server.Services;

public class UserExerciseInfoService
{
    private readonly ExerciseRepository exerciseRepository;

    public UserExerciseInfoService(ExerciseRepository exerciseRepository)
    {
        this.exerciseRepository = exerciseRepository;
    }

    public async Task<List<UserExerciseInfo>> GetByIdsAsync(Guid? userId, List<Guid> exerciseInfoIds, Guid? excludeWorkoutId = null)
    {
        if (userId == null) return new List<UserExerciseInfo>();

        var latestExercises = await exerciseRepository.GetLatestExercisesForUserAsync(userId.Value, exerciseInfoIds, excludeWorkoutId);

        return latestExercises
            .Select(e => new UserExerciseInfo()
            {
                ExerciseInfo = e.ExerciseInfo,
                LatestExercise = e
            }).ToList();
    }
}
