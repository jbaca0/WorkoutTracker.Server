using System;
using WorkoutTracker.Server.DomainObjects;
using WorkoutTracker.Server.Models.Repositories;

namespace WorkoutTracker.Server.Services;

public class UserExerciseInfoService
{
    private readonly ExerciseRepository exerciseRepository;
    private readonly ExerciseGoalRepository exerciseGoalRepository;
    private readonly WorkoutPlanRepository workoutPlanRepository;

    public UserExerciseInfoService(
        ExerciseRepository exerciseRepository,
        ExerciseGoalRepository exerciseGoalRepository,
        WorkoutPlanRepository workoutPlanRepository)
    {
        this.exerciseRepository = exerciseRepository;
        this.exerciseGoalRepository = exerciseGoalRepository;
        this.workoutPlanRepository = workoutPlanRepository;
    }

    public async Task<List<UserExerciseInfo>> GetByIdsAsync(Guid? userId, List<Guid> exerciseInfoIds, Guid? excludeWorkoutId = null)
    {
        if (userId == null) return new List<UserExerciseInfo>();

        var exerciseInfos = await exerciseRepository.GetExerciseInfosByIdsAsync(exerciseInfoIds);
        var latestExercises = await exerciseRepository.GetLatestExercisesForUserAsync(userId.Value, exerciseInfoIds, excludeWorkoutId);
        var latestByInfoId = latestExercises.ToDictionary(e => e.ExerciseInfoId);

        var goals = await exerciseGoalRepository.GetByExerciseInfoIdsAsync(userId.Value, exerciseInfoIds);
        var goalsByInfoId = goals.ToDictionary(g => g.ExerciseInfoId);

        var planCache = new Dictionary<Guid, DomainObjects.WorkoutPlan?>();

        var result = new List<UserExerciseInfo>();
        foreach (var ei in exerciseInfos)
        {
            var bestSet = await exerciseRepository.GetBestSetByEpleyAsync(userId.Value, ei.Id);

            DataTransferObjects.Common.GoalRecommendation? recommendation = null;
            if (goalsByInfoId.TryGetValue(ei.Id, out var goal))
            {
                if (!planCache.TryGetValue(goal.WorkoutPlanId, out var plan))
                {
                    plan = await workoutPlanRepository.GetByIdWithDetailsAsync(goal.WorkoutPlanId);
                    planCache[goal.WorkoutPlanId] = plan;
                }

                if (plan != null)
                {
                    recommendation = goal.CalculateRecommendation(DateTime.UtcNow, plan);
                }
            }

            result.Add(new UserExerciseInfo
            {
                ExerciseInfo = ei,
                LatestExercise = latestByInfoId.TryGetValue(ei.Id, out var le) ? le : null,
                BestSetWeight = bestSet?.weight,
                BestSetReps = bestSet?.reps,
                GoalRecommendation = recommendation
            });
        }

        return result;
    }
}
