using System;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace WorkoutTracker.Server.Models;

public class ApplicationDbContext : IdentityDbContext<IdentityUser>
{
    public DbSet<Workout> Workouts { get; set; }
    public DbSet<ExerciseInfo> ExerciseInfos { get; set; }
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) :
        base(options)
    { }

    protected override void OnConfiguring(DbContextOptionsBuilder options)
        => options.UseSqlite(@"Data Source=./WorkoutTracker.db");

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.Entity<Workout>()
            .Property(w => w.AddDateTime)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");
    }
}
