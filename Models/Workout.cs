using System;

namespace WorkoutTracker.Server.Models
{
    public class Workout
    {
        public Guid Id { get; set; }
        public DateTime AddDateTime { get; set; }
        public Guid UserId { get; set; }

        public ICollection<Exercise> Exercises { get; set; } = new List<Exercise>();
    }
}
