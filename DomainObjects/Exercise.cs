using System;

namespace WorkoutTracker.Server.DomainObjects;

public class Exercise
{
    public Guid Id { get; set; }
    public Guid WorkoutId { get; set; }
    public Guid ExerciseInfoId { get; set; }
    public required ExerciseInfo ExerciseInfo { get; set; }
    public List<Set> Sets { get; set; } = new List<Set>();

    public static Exercise FromModel(Models.Exercise model)
    {
        return new Exercise
        {
            Id = model.Id,
            WorkoutId = model.WorkoutId,
            ExerciseInfoId = model.ExerciseInfoId,
            ExerciseInfo = ExerciseInfo.FromModel(model.ExerciseInfo),
            Sets = model.Sets?.Select(Set.FromModel).ToList() ?? new List<Set>()
        };
    }

    public Models.Exercise ToModel()
    {
        return new Models.Exercise
        {
            Id = Id,
            WorkoutId = WorkoutId,
            ExerciseInfoId = ExerciseInfoId,
            ExerciseInfo = ExerciseInfo?.ToModel()!,
            Sets = Sets.Select(s => s.ToModel()).ToList()
        };
    }

    public static Exercise FromData(DataTransferObjects.Common.Exercise exercise)
    {
        return new Exercise
        {
            ExerciseInfoId = exercise.exerciseInfoId,
            ExerciseInfo = new ExerciseInfo { Id = exercise.exerciseInfoId }, // Minimal ExerciseInfo, may need to be populated separately
            Sets = exercise.sets?.Select(Set.FromData).ToList() ?? new List<Set>()
        };
    }
}