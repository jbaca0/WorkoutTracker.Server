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

        var exerciseInfos = await exerciseRepository.GetExerciseInfosByIdsAsync(exerciseInfoIds);
        var latestExercises = await exerciseRepository.GetLatestExercisesForUserAsync(userId.Value, exerciseInfoIds, excludeWorkoutId);
        var latestByInfoId = latestExercises.ToDictionary(e => e.ExerciseInfoId);

        return exerciseInfos.Select(ei => new UserExerciseInfo
        {
            ExerciseInfo = ei,
            LatestExercise = latestByInfoId.TryGetValue(ei.Id, out var le) ? le : null
        }).ToList();
    }
}
