using System;

namespace WorkoutTracker.Server.DomainObjects;

public class ExerciseGoal
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid ExerciseInfoId { get; set; }
    public ExerciseInfo? ExerciseInfo { get; set; }
    public Guid WorkoutPlanId { get; set; }
    public decimal StartWeight { get; set; }
    public int StartReps { get; set; }
    public decimal TargetWeight { get; set; }
    public int TargetReps { get; set; }
    public DateTime GoalDate { get; set; }
    public DateTime CreatedAt { get; set; }

    public static ExerciseGoal FromModel(Models.ExerciseGoal model)
    {
        return new ExerciseGoal
        {
            Id = model.Id,
            UserId = model.UserId,
            ExerciseInfoId = model.ExerciseInfoId,
            ExerciseInfo = model.ExerciseInfo != null ? DomainObjects.ExerciseInfo.FromModel(model.ExerciseInfo) : null,
            WorkoutPlanId = model.WorkoutPlanId,
            StartWeight = model.StartWeight,
            StartReps = model.StartReps,
            TargetWeight = model.TargetWeight,
            TargetReps = model.TargetReps,
            GoalDate = model.GoalDate,
            CreatedAt = model.CreatedAt
        };
    }

    public DataTransferObjects.Common.ExerciseGoal ToData(DataTransferObjects.Common.GoalRecommendation? recommendation = null)
    {
        return new DataTransferObjects.Common.ExerciseGoal
        {
            id = Id,
            exerciseInfoId = ExerciseInfoId,
            exerciseInfo = ExerciseInfo != null ? new DataTransferObjects.Common.ExerciseInfo
            {
                id = ExerciseInfo.Id,
                name = ExerciseInfo.Name
            } : null,
            workoutPlanId = WorkoutPlanId,
            startWeight = StartWeight,
            startReps = StartReps,
            targetWeight = TargetWeight,
            targetReps = TargetReps,
            goalDate = GoalDate,
            goalRecommendation = recommendation
        };
    }

    /// <summary>
    /// Computes the recommended weight/reps for today's session by linearly interpolating
    /// the estimated 1RM (Epley) between the goal's start and target across the plan's
    /// scheduled occurrences of this exercise.
    /// </summary>
    public DataTransferObjects.Common.GoalRecommendation CalculateRecommendation(DateTime today, WorkoutPlan plan)
    {
        var start1RM = StartWeight * (1 + StartReps / 30m);
        var target1RM = TargetWeight * (1 + TargetReps / 30m);

        var planStart = plan.StartDate.Date;
        var goalDate = GoalDate.Date;
        var todayDate = today.Date;

        var dayMap = plan.Days
            .Where(d => d.WorkoutTemplate != null)
            .GroupBy(d => d.DayOfWeek)
            .ToDictionary(g => g.Key, g => g.First());

        int totalOccurrences = 0;
        if (goalDate >= planStart)
        {
            for (var date = planStart; date <= goalDate; date = date.AddDays(1))
            {
                if (TryGetTargetReps(dayMap, date, ExerciseInfoId, out _))
                {
                    totalOccurrences++;
                }
            }
        }

        decimal current1RM;
        if (totalOccurrences == 0)
        {
            current1RM = target1RM;
        }
        else
        {
            var step1RM = (target1RM - start1RM) / totalOccurrences;

            int sessionIndex = 0;
            var sessionEnd = todayDate < planStart ? planStart.AddDays(-1) : todayDate;
            for (var date = planStart; date <= sessionEnd; date = date.AddDays(1))
            {
                if (TryGetTargetReps(dayMap, date, ExerciseInfoId, out _))
                {
                    sessionIndex++;
                }
            }

            current1RM = Math.Min(start1RM + step1RM * sessionIndex, target1RM);
        }

        int plannedReps = StartReps > 0 ? StartReps : 1;
        for (var date = todayDate; date <= goalDate; date = date.AddDays(1))
        {
            if (TryGetTargetReps(dayMap, date, ExerciseInfoId, out var reps))
            {
                plannedReps = reps;
                break;
            }
        }

        var recommendedWeight = Math.Round(current1RM / (1 + plannedReps / 30m) / 5m) * 5m;

        return new DataTransferObjects.Common.GoalRecommendation
        {
            weight = recommendedWeight,
            reps = plannedReps
        };
    }

    private static bool TryGetTargetReps(Dictionary<int, WorkoutPlanDay> dayMap, DateTime date, Guid exerciseInfoId, out int targetReps)
    {
        targetReps = 0;
        if (dayMap.TryGetValue((int)date.DayOfWeek, out var day) && day.WorkoutTemplate != null)
        {
            var exercise = day.WorkoutTemplate.Exercises.FirstOrDefault(e => e.ExerciseInfoId == exerciseInfoId);
            if (exercise != null)
            {
                targetReps = exercise.TargetReps;
                return true;
            }
        }
        return false;
    }
}
