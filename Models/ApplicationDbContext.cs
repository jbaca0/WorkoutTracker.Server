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
    public DbSet<WorkoutTemplate> WorkoutTemplates { get; set; }
    public DbSet<WorkoutTemplateExercise> WorkoutTemplateExercises { get; set; }
    public DbSet<WorkoutPlan> WorkoutPlans { get; set; }
    public DbSet<WorkoutPlanDay> WorkoutPlanDays { get; set; }
    public DbSet<ExerciseGoal> ExerciseGoals { get; set; }

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

        builder.Entity<WorkoutTemplate>()
            .Property(t => t.CreatedDateTime)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Entity<WorkoutTemplate>()
            .HasMany(t => t.Exercises)
            .WithOne(e => e.WorkoutTemplate)
            .HasForeignKey(e => e.WorkoutTemplateId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<WorkoutPlan>()
            .Property(p => p.CreatedDateTime)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Entity<WorkoutPlan>()
            .Property(p => p.StartDate)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Entity<WorkoutPlan>()
            .HasMany(p => p.Days)
            .WithOne(d => d.WorkoutPlan)
            .HasForeignKey(d => d.WorkoutPlanId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<WorkoutTemplateExercise>()
            .Property(e => e.TargetReps)
            .HasDefaultValue(0);

        builder.Entity<ExerciseGoal>()
            .Property(g => g.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Entity<ExerciseGoal>()
            .HasIndex(g => new { g.UserId, g.ExerciseInfoId })
            .IsUnique();

        builder.Entity<ExerciseGoal>()
            .HasOne(g => g.WorkoutPlan)
            .WithMany()
            .HasForeignKey(g => g.WorkoutPlanId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<ExerciseGoal>()
            .HasOne(g => g.ExerciseInfo)
            .WithMany()
            .HasForeignKey(g => g.ExerciseInfoId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
