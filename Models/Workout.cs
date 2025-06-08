using System;

namespace WorkoutTracker.Server.Models
{
    public class Workout
    {
        public Guid Id { get; set; }
        public DateTime AddDateTime { get; set; }
        public Guid UserId { get; set; }
    }
}
