using System;
using WorkoutTracker.Server.DomainObjects;
using WorkoutTracker.Server.Enums;
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
            var latestExercise = latestByInfoId.TryGetValue(ei.Id, out var le) ? le : null;

            DataTransferObjects.Common.ExerciseTarget? target = null;
            if (goalsByInfoId.TryGetValue(ei.Id, out var goal))
            {
                if (!planCache.TryGetValue(goal.WorkoutPlanId, out var plan))
                {
                    plan = await workoutPlanRepository.GetByIdWithDetailsAsync(goal.WorkoutPlanId);
                    planCache[goal.WorkoutPlanId] = plan;
                }

                if (plan != null)
                {
                    var recommendation = goal.CalculateRecommendation(DateTime.UtcNow, plan);
                    var setCount = latestExercise?.Sets.Count > 0 ? latestExercise.Sets.Count : 1;
                    target = new DataTransferObjects.Common.ExerciseTarget
                    {
                        type = TargetSource.Plan,
                        sets = Enumerable.Range(0, setCount)
                            .Select(_ => new DataTransferObjects.Common.TargetSet { weight = recommendation.weight, reps = recommendation.reps })
                            .ToList()
                    };
                }
            }

            if (target == null && latestExercise != null && latestExercise.Sets.Count > 0)
            {
                target = new DataTransferObjects.Common.ExerciseTarget
                {
                    type = TargetSource.Previous,
                    sets = latestExercise.Sets
                        .Select(s => new DataTransferObjects.Common.TargetSet { weight = s.Weight, reps = s.Reps })
                        .ToList()
                };
            }

            result.Add(new UserExerciseInfo
            {
                ExerciseInfo = ei,
                LatestExercise = latestExercise,
                BestSetWeight = bestSet?.weight,
                BestSetReps = bestSet?.reps,
                Target = target
            });
        }

        return result;
    }
}
