using System;

namespace WorkoutTracker.Server.DomainObjects;

public class Workout
{
    public Guid Id { get; set; }
    public DateTime AddDateTime { get; set; }
    public Guid UserId { get; set; }
    public List<Exercise> Exercises { get; set; } = new List<Exercise>();

    public Models.Workout ToModel()
    {
        return new Models.Workout
        {
            Id = Id,
            AddDateTime = AddDateTime,
            UserId = UserId,
            Exercises = Exercises.Select(e => e.ToModel()).ToList()
        };
    }

    public static Workout FromModel(Models.Workout model)
    {
        return new Workout
        {
            Id = model.Id,
            AddDateTime = model.AddDateTime,
            UserId = model.UserId,
            Exercises = model.Exercises?.Select(Exercise.FromModel).ToList() ?? new List<Exercise>()
        };
    }

    public static Workout FromData(DataTransferObjects.Common.Workout workout)
    {
        return new Workout
        {
            Id = workout.id,
            AddDateTime = workout.addDateTime,
            Exercises = workout.exercises?.Select(e => DomainObjects.Exercise.FromData(e)).ToList() ?? new List<Exercise>()
            // UserId would need to come from another source since it's not in the DTO
        };
    }
}
