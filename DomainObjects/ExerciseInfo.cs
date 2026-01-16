using System;

namespace WorkoutTracker.Server.DomainObjects;

public class ExerciseInfo
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public static ExerciseInfo FromModel(Models.ExerciseInfo model)
    {
        return new ExerciseInfo
        {
            Id = model.Id,
            Name = model.Name
        };
    }

    public Models.ExerciseInfo ToModel()
    {
        return new Models.ExerciseInfo
        {
            Id = Id,
            Name = Name
        };
    }
}