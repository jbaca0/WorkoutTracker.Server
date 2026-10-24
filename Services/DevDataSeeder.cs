using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WorkoutTracker.Server.Enums;
using WorkoutTracker.Server.Models;

namespace WorkoutTracker.Server.Services;

/// <summary>
/// Development-only seeding of a test user with sample workout data.
/// Credentials come from DevSeed:Email / DevSeed:Password, which live in the
/// gitignored appsettings.Development.local.json. Nothing runs unless both are set.
/// </summary>
public static class DevDataSeeder
{
    public static async Task SeedDevUserAsync(IServiceProvider services, IConfiguration configuration, ILogger logger)
    {
        var email = configuration["DevSeed:Email"];
        var password = configuration["DevSeed:Password"];
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            return;
        }

        var userManager = services.GetRequiredService<UserManager<IdentityUser>>();
        var user = await userManager.FindByEmailAsync(email);
        if (user == null)
        {
            user = new IdentityUser { UserName = email, Email = email };
            var result = await userManager.CreateAsync(user, password);
            if (!result.Succeeded)
            {
                logger.LogWarning("Dev seed user creation failed: {Errors}",
                    string.Join(", ", result.Errors.Select(e => e.Description)));
                return;
            }
        }

        var context = services.GetRequiredService<ApplicationDbContext>();
        var userId = Guid.Parse(user.Id);

        // Sample data is only seeded once; delete the user's workouts to re-seed.
        if (await context.Workouts.AnyAsync(w => w.UserId == userId))
        {
            return;
        }

        await SeedSampleDataAsync(context, userId);
        logger.LogInformation("Seeded dev user {Email} with sample data", email);
    }

    private static async Task SeedSampleDataAsync(ApplicationDbContext context, Guid userId)
    {
        var exercises = await GetOrCreateExerciseInfosAsync(context, userId);

        var push = CreateTemplate(userId, "Push Day",
            (exercises["Bench Press"], new[] { 5, 5, 5 }),
            (exercises["Overhead Press"], new[] { 8, 8, 8 }),
            (exercises["Tricep Pushdown"], new[] { 12, 12, 12 }));
        var pull = CreateTemplate(userId, "Pull Day",
            (exercises["Deadlift"], new[] { 5, 5, 5 }),
            (exercises["Barbell Row"], new[] { 8, 8, 8 }),
            (exercises["Pull Up"], new[] { 10, 10, 10 }));
        var legs = CreateTemplate(userId, "Leg Day",
            (exercises["Squat"], new[] { 5, 5, 5 }),
            (exercises["Romanian Deadlift"], new[] { 10, 10, 10 }),
            (exercises["Plank"], new[] { 60, 60 }));
        context.WorkoutTemplates.AddRange(push, pull, legs);

        // Push/Pull/Legs on Mon/Wed/Fri, starting six weeks back on a Monday.
        var today = DateTime.UtcNow.Date;
        var daysSinceMonday = ((int)today.DayOfWeek + 6) % 7;
        var planStart = today.AddDays(-daysSinceMonday - 42);

        var plan = new WorkoutPlan
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = "PPL Strength",
            IsActive = true,
            StartDate = planStart,
            EndDate = planStart.AddDays(12 * 7),
        };
        plan.Days.Add(new WorkoutPlanDay { Id = Guid.NewGuid(), DayOfWeek = 1, WorkoutTemplateId = push.Id });
        plan.Days.Add(new WorkoutPlanDay { Id = Guid.NewGuid(), DayOfWeek = 3, WorkoutTemplateId = pull.Id });
        plan.Days.Add(new WorkoutPlanDay { Id = Guid.NewGuid(), DayOfWeek = 5, WorkoutTemplateId = legs.Id });
        plan.Days.Add(new WorkoutPlanDay { Id = Guid.NewGuid(), DayOfWeek = 6, Note = "Optional cardio" });
        context.WorkoutPlans.Add(plan);

        context.ExerciseGoals.AddRange(
            CreateGoal(userId, exercises["Bench Press"], plan, 135m, 5, 185m, 5),
            CreateGoal(userId, exercises["Squat"], plan, 185m, 5, 245m, 5));

        // Starting working weights per exercise, and how much they go up each week.
        var progression = new Dictionary<string, (decimal Start, decimal WeeklyIncrease)>
        {
            ["Bench Press"] = (135m, 5m),
            ["Overhead Press"] = (75m, 2.5m),
            ["Tricep Pushdown"] = (40m, 2.5m),
            ["Deadlift"] = (225m, 10m),
            ["Barbell Row"] = (115m, 5m),
            ["Pull Up"] = (0m, 0m),
            ["Squat"] = (185m, 10m),
            ["Romanian Deadlift"] = (135m, 5m),
            ["Plank"] = (0m, 0m),
        };

        var rotation = new (WorkoutTemplate Template, int DayOffset)[] { (push, 0), (pull, 2), (legs, 4) };
        for (var week = 0; week < 6; week++)
        {
            foreach (var (template, dayOffset) in rotation)
            {
                // Skip one session so the history has a realistic gap.
                if (week == 3 && template == pull)
                {
                    continue;
                }

                var workout = new Workout
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    AddDateTime = planStart.AddDays(week * 7 + dayOffset).AddHours(17).AddMinutes(30),
                };

                foreach (var templateExercise in template.Exercises.OrderBy(e => e.Order))
                {
                    var name = exercises.First(e => e.Value.Id == templateExercise.ExerciseInfoId).Key;
                    var (start, increase) = progression[name];
                    var weight = start + increase * week;

                    var exercise = new Exercise { Id = Guid.NewGuid(), ExerciseInfoId = templateExercise.ExerciseInfoId };
                    foreach (var templateSet in templateExercise.Sets.OrderBy(s => s.Order))
                    {
                        exercise.Sets.Add(new Set
                        {
                            Id = Guid.NewGuid(),
                            Reps = templateSet.Reps,
                            Weight = weight,
                            Completed = true,
                            Order = templateSet.Order,
                        });
                    }
                    workout.Exercises.Add(exercise);
                }

                context.Workouts.Add(workout);
            }
        }

        await context.SaveChangesAsync();
    }

    private static async Task<Dictionary<string, ExerciseInfo>> GetOrCreateExerciseInfosAsync(ApplicationDbContext context, Guid userId)
    {
        var wanted = new (string Name, MuscleGroup Group)[]
        {
            ("Bench Press", MuscleGroup.Chest),
            ("Overhead Press", MuscleGroup.Shoulders),
            ("Tricep Pushdown", MuscleGroup.Triceps),
            ("Deadlift", MuscleGroup.Back),
            ("Barbell Row", MuscleGroup.Back),
            ("Pull Up", MuscleGroup.Back),
            ("Squat", MuscleGroup.Legs),
            ("Romanian Deadlift", MuscleGroup.Legs),
            ("Plank", MuscleGroup.Core),
        };

        // Reuse anything already visible to this user (global or their own); create the rest as theirs.
        var visible = await context.ExerciseInfos
            .Where(e => e.CreatedByUserId == null || e.CreatedByUserId == userId)
            .ToListAsync();

        var result = new Dictionary<string, ExerciseInfo>();
        foreach (var (name, group) in wanted)
        {
            var existing = visible.FirstOrDefault(e => e.Name == name);
            if (existing == null)
            {
                existing = new ExerciseInfo
                {
                    Id = Guid.NewGuid(),
                    Name = name,
                    PrimaryMuscleGroup = group,
                    SecondaryMuscleGroupsJson = "[]",
                    CreatedByUserId = userId,
                };
                context.ExerciseInfos.Add(existing);
            }
            result[name] = existing;
        }

        return result;
    }

    private static WorkoutTemplate CreateTemplate(Guid userId, string name, params (ExerciseInfo Info, int[] Reps)[] exercises)
    {
        var template = new WorkoutTemplate { Id = Guid.NewGuid(), UserId = userId, Name = name };
        var order = 1;
        foreach (var (info, reps) in exercises)
        {
            var templateExercise = new WorkoutTemplateExercise
            {
                Id = Guid.NewGuid(),
                ExerciseInfoId = info.Id,
                Order = order++,
            };
            for (var i = 0; i < reps.Length; i++)
            {
                templateExercise.Sets.Add(new WorkoutTemplateExerciseSet { Id = Guid.NewGuid(), Order = i + 1, Reps = reps[i] });
            }
            template.Exercises.Add(templateExercise);
        }
        return template;
    }

    private static ExerciseGoal CreateGoal(Guid userId, ExerciseInfo info, WorkoutPlan plan,
        decimal startWeight, int startReps, decimal targetWeight, int targetReps)
    {
        return new ExerciseGoal
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ExerciseInfoId = info.Id,
            WorkoutPlanId = plan.Id,
            StartWeight = startWeight,
            StartReps = startReps,
            TargetWeight = targetWeight,
            TargetReps = targetReps,
            GoalDate = plan.EndDate!.Value,
        };
    }
}
