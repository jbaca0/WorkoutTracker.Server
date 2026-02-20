using System;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WorkoutTracker.Server.Enums;

namespace WorkoutTracker.Server.Models;

public class ApplicationDbContext : IdentityDbContext<IdentityUser>
{
    public DbSet<Workout> Workouts { get; set; }
    public DbSet<ExerciseInfo> ExerciseInfos { get; set; }
    public DbSet<Exercise> Exercises { get; set; }
    public DbSet<Set> Sets { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<LogEntry> LogEntries { get; set; }

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) :
        base(options)
    { }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.Entity<Workout>()
            .Property(w => w.AddDateTime)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Entity<LogEntry>()
            .Property(l => l.Timestamp)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Entity<LogEntry>()
            .HasIndex(l => l.Timestamp);

        builder.Entity<ExerciseInfo>()
            .Property(e => e.PrimaryMuscleGroup)
            .HasConversion<int>()
            .HasDefaultValue(MuscleGroup.None);

        builder.Entity<ExerciseInfo>()
            .Property(e => e.SecondaryMuscleGroupsJson)
            .HasDefaultValue("[]");
    }
}
