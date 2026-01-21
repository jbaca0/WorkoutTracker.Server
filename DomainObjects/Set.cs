using System;

namespace WorkoutTracker.Server.DomainObjects;

public class Set
{
    public Guid Id { get; set; }
    public Guid ExerciseId { get; set; }
    public int Reps { get; set; }
    public decimal Weight { get; set; }

    public static Set FromModel(Models.Set model)
    {
        return new Set
        {
            Id = model.Id,
            ExerciseId = model.ExerciseId,
            Reps = model.Reps,
            Weight = model.Weight
        };
    }

    public Models.Set ToModel()
    {
        return new Models.Set
        {
            Id = Id,
            ExerciseId = ExerciseId,
            Reps = Reps,
            Weight = Weight
        };
    }

    public static Set FromData(DataTransferObjects.Common.Set data)
    {
        return new Set
        {
            Reps = data.reps,
            Weight = data.weight
        };
    }

    public DataTransferObjects.Common.Set ToData()
    {
        return new DataTransferObjects.Common.Set
        {
            id = Id,
            reps = Reps,
            weight = Weight
        };
    }
}