using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using WorkoutTracker.Server.DataTransferObjects.Requests;
using WorkoutTracker.Server.DataTransferObjects.Responses;
using WorkoutTracker.Server.DataTransferObjects.Responses.Workout;
using WorkoutTracker.Server.Handlers;
using WorkoutTracker.Server.Models.Repositories;
using WorkoutTracker.Server.Services;
using WorkoutTracker.Server.Tests.Helpers;
using Xunit;

namespace WorkoutTracker.Server.Tests.Handlers;

public class WorkoutHandlerTests
{
    private static DefaultHttpContext HttpContextForUser(Guid userId) =>
        new() { User = ClaimsPrincipalBuilder.WithUserId(userId) };

    private static Models.ExerciseInfo MakeExerciseInfo(string name = "Bench Press") =>
        new() { Id = Guid.NewGuid(), Name = name, SecondaryMuscleGroupsJson = "[]" };

    // --- AddWorkout ---

    [Fact]
    public async Task AddWorkout_CreatesWorkoutForCurrentUser_AndReturnsItsId()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var userId = Guid.NewGuid();

        var result = await WorkoutHandler.AddWorkout(HttpContextForUser(userId), dbContext);

        var ok = result.Should().BeOfType<Ok<Guid>>().Subject;
        dbContext.Workouts.Should().ContainSingle();
        var workout = dbContext.Workouts.Single();
        workout.Id.Should().Be(ok.Value);
        workout.UserId.Should().Be(userId);
    }

    // --- GetWorkouts ---

    [Fact]
    public async Task GetWorkouts_ReturnsOnlyWorkoutIdsBelongingToCurrentUser()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var userId = Guid.NewGuid();
        var myWorkout = new Models.Workout { Id = Guid.NewGuid(), UserId = userId };
        var otherWorkout = new Models.Workout { Id = Guid.NewGuid(), UserId = Guid.NewGuid() };
        dbContext.Workouts.AddRange(myWorkout, otherWorkout);
        await dbContext.SaveChangesAsync();

        var result = await WorkoutHandler.GetWorkouts(HttpContextForUser(userId), dbContext);

        var ok = result.Should().BeOfType<Ok<List<Guid>>>().Subject;
        ok.Value.Should().ContainSingle().Which.Should().Be(myWorkout.Id);
    }

    // --- GetExercises ---

    [Fact]
    public async Task GetExercises_ReturnsExerciseNamesForWorkout()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var workout = new Models.Workout { Id = Guid.NewGuid(), UserId = Guid.NewGuid() };
        var exerciseInfo = MakeExerciseInfo("Squat");
        dbContext.Workouts.Add(workout);
        dbContext.ExerciseInfos.Add(exerciseInfo);
        dbContext.Exercises.Add(new Models.Exercise
        {
            Id = Guid.NewGuid(),
            WorkoutId = workout.Id,
            ExerciseInfoId = exerciseInfo.Id
        });
        await dbContext.SaveChangesAsync();

        var result = await WorkoutHandler.GetExercises(workout.Id, new WorkoutRepository(dbContext));

        var ok = result.Should().BeOfType<Ok<List<string>>>().Subject;
        ok.Value.Should().ContainSingle().Which.Should().Be("Squat");
    }

    [Fact]
    public async Task GetExercises_WithUnknownWorkout_ReturnsEmptyList()
    {
        var dbContext = DbContextFactory.CreateInMemory();

        var result = await WorkoutHandler.GetExercises(Guid.NewGuid(), new WorkoutRepository(dbContext));

        var ok = result.Should().BeOfType<Ok<List<string>>>().Subject;
        ok.Value.Should().BeEmpty();
    }

    // --- AddExercise ---

    [Fact]
    public async Task AddExercise_WithValidIds_AddsExerciseAndReturnsItsId()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var workout = new Models.Workout { Id = Guid.NewGuid(), UserId = Guid.NewGuid() };
        var exerciseInfo = MakeExerciseInfo();
        dbContext.Workouts.Add(workout);
        dbContext.ExerciseInfos.Add(exerciseInfo);
        await dbContext.SaveChangesAsync();

        var request = new AddExerciseRequest { workoutId = workout.Id, exerciseInfoId = exerciseInfo.Id };
        var result = await WorkoutHandler.AddExercise(dbContext, request);

        var ok = result.Should().BeOfType<Ok<AddExerciseResponse>>().Subject;
        dbContext.Exercises.Should().ContainSingle();
        var exercise = dbContext.Exercises.Single();
        exercise.Id.Should().Be(ok.Value!.exerciseId);
        exercise.WorkoutId.Should().Be(workout.Id);
        exercise.ExerciseInfoId.Should().Be(exerciseInfo.Id);
    }

    [Fact]
    public async Task AddExercise_WithUnknownWorkoutId_ReturnsNotFound()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var exerciseInfo = MakeExerciseInfo();
        dbContext.ExerciseInfos.Add(exerciseInfo);
        await dbContext.SaveChangesAsync();

        var request = new AddExerciseRequest { workoutId = Guid.NewGuid(), exerciseInfoId = exerciseInfo.Id };
        var result = await WorkoutHandler.AddExercise(dbContext, request);

        result.Should().BeOfType<NotFound<string>>();
    }

    [Fact]
    public async Task AddExercise_WithUnknownExerciseInfoId_ReturnsNotFound()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var workout = new Models.Workout { Id = Guid.NewGuid(), UserId = Guid.NewGuid() };
        dbContext.Workouts.Add(workout);
        await dbContext.SaveChangesAsync();

        var request = new AddExerciseRequest { workoutId = workout.Id, exerciseInfoId = Guid.NewGuid() };
        var result = await WorkoutHandler.AddExercise(dbContext, request);

        result.Should().BeOfType<NotFound<string>>();
    }

    // --- AddExercises ---

    [Fact]
    public async Task AddExercises_WithNullExerciseInfoIds_ReturnsBadRequest()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var request = new AddExercisesRequest { workoutId = Guid.NewGuid(), exerciseInfoIds = null };

        var result = await WorkoutHandler.AddExercises(
            request,
            ClaimsPrincipalBuilder.WithUserId(Guid.NewGuid()),
            new WorkoutRepository(dbContext),
            new UserExerciseInfoService(new ExerciseRepository(dbContext)));

        result.Should().BeOfType<BadRequest>();
    }

    [Fact]
    public async Task AddExercises_WithUnknownWorkout_ReturnsBadRequest()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var request = new AddExercisesRequest
        {
            workoutId = Guid.NewGuid(),
            exerciseInfoIds = new List<Guid> { Guid.NewGuid() }
        };

        var result = await WorkoutHandler.AddExercises(
            request,
            ClaimsPrincipalBuilder.WithUserId(Guid.NewGuid()),
            new WorkoutRepository(dbContext),
            new UserExerciseInfoService(new ExerciseRepository(dbContext)));

        result.Should().BeOfType<BadRequest>();
    }

    [Fact]
    public async Task AddExercises_WithValidRequest_AddsExercisesAndReturnsUserExerciseInfos()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var userId = Guid.NewGuid();
        var exerciseInfo = MakeExerciseInfo("Deadlift");
        var workout = new Models.Workout { Id = Guid.NewGuid(), UserId = userId };
        dbContext.ExerciseInfos.Add(exerciseInfo);
        dbContext.Workouts.Add(workout);
        await dbContext.SaveChangesAsync();

        var request = new AddExercisesRequest
        {
            workoutId = workout.Id,
            exerciseInfoIds = new List<Guid> { exerciseInfo.Id }
        };

        var result = await WorkoutHandler.AddExercises(
            request,
            ClaimsPrincipalBuilder.WithUserId(userId),
            new WorkoutRepository(dbContext),
            new UserExerciseInfoService(new ExerciseRepository(dbContext)));

        var ok = result.Should().BeOfType<Ok<AddExercisesResponse>>().Subject;
        ok.Value!.userExerciseInfos.Should().ContainSingle();
        ok.Value.userExerciseInfos[0].exerciseInfo.name.Should().Be("Deadlift");
        dbContext.Exercises.Should().ContainSingle(e => e.WorkoutId == workout.Id);
    }

    [Fact]
    public async Task AddExercises_ReturnsLatestExerciseFromPreviousWorkout_ExcludingCurrentWorkout()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var userId = Guid.NewGuid();
        var exerciseInfo = MakeExerciseInfo();

        var previousWorkout = new Models.Workout
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            AddDateTime = DateTime.UtcNow.AddDays(-1)
        };
        var previousExercise = new Models.Exercise
        {
            Id = Guid.NewGuid(),
            WorkoutId = previousWorkout.Id,
            ExerciseInfoId = exerciseInfo.Id
        };
        previousExercise.Sets.Add(new Models.Set
        {
            Id = Guid.NewGuid(),
            ExerciseId = previousExercise.Id,
            Reps = 8,
            Weight = 185m,
            Order = 1
        });

        var currentWorkout = new Models.Workout { Id = Guid.NewGuid(), UserId = userId, AddDateTime = DateTime.UtcNow };
        dbContext.ExerciseInfos.Add(exerciseInfo);
        dbContext.Workouts.AddRange(previousWorkout, currentWorkout);
        dbContext.Exercises.Add(previousExercise);
        await dbContext.SaveChangesAsync();

        var request = new AddExercisesRequest
        {
            workoutId = currentWorkout.Id,
            exerciseInfoIds = new List<Guid> { exerciseInfo.Id }
        };

        var result = await WorkoutHandler.AddExercises(
            request,
            ClaimsPrincipalBuilder.WithUserId(userId),
            new WorkoutRepository(dbContext),
            new UserExerciseInfoService(new ExerciseRepository(dbContext)));

        var ok = result.Should().BeOfType<Ok<AddExercisesResponse>>().Subject;
        var info = ok.Value!.userExerciseInfos.Should().ContainSingle().Subject;
        info.latestExercise.Should().NotBeNull();
        info.latestExercise!.sets.Should().ContainSingle();
        info.latestExercise.sets[0].reps.Should().Be(8);
        info.latestExercise.sets[0].weight.Should().Be(185m);
    }

    // --- Save ---

    [Fact]
    public async Task Save_WithNewWorkout_PersistsWorkoutAndReturnsOk()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var workoutId = Guid.NewGuid();
        var exerciseInfoId = Guid.NewGuid();

        var request = new SaveRequest
        {
            workoutId = workoutId,
            workout = new DataTransferObjects.Common.Workout
            {
                id = workoutId,
                addDateTime = DateTime.UtcNow,
                exercises = new List<DataTransferObjects.Common.Exercise>
                {
                    new()
                    {
                        exerciseInfoId = exerciseInfoId,
                        sets = new List<DataTransferObjects.Common.Set>
                        {
                            new() { reps = 10, weight = 100m, completed = true }
                        }
                    }
                }
            }
        };

        var result = await WorkoutHandler.Save(
            request,
            ClaimsPrincipalBuilder.WithUserId(Guid.NewGuid()),
            new WorkoutRepository(dbContext));

        result.Should().BeOfType<Ok>();
        dbContext.Workouts.Should().ContainSingle(w => w.Id == workoutId);
        dbContext.Exercises.Should().ContainSingle(e => e.WorkoutId == workoutId);
        dbContext.Sets.Should().ContainSingle(s => s.Reps == 10);
    }

    [Fact]
    public async Task Save_WhenRepositorySaveFails_ReturnsBadRequest()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var repo = new WorkoutRepository(dbContext);
        await dbContext.DisposeAsync(); // force the repository save to fail

        var request = new SaveRequest
        {
            workoutId = Guid.NewGuid(),
            workout = new DataTransferObjects.Common.Workout { id = Guid.NewGuid() }
        };

        var result = await WorkoutHandler.Save(
            request,
            ClaimsPrincipalBuilder.WithUserId(Guid.NewGuid()),
            repo);

        result.Should().BeOfType<BadRequest>();
    }

    // --- GetWorkoutHistory ---

    [Fact]
    public async Task GetWorkoutHistory_ReturnsSummariesForCurrentUserOnly()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var userId = Guid.NewGuid();
        var exerciseInfo = MakeExerciseInfo("Squat");
        var myWorkout = new Models.Workout { Id = Guid.NewGuid(), UserId = userId, AddDateTime = DateTime.UtcNow };
        var otherWorkout = new Models.Workout { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), AddDateTime = DateTime.UtcNow };
        dbContext.ExerciseInfos.Add(exerciseInfo);
        dbContext.Workouts.AddRange(myWorkout, otherWorkout);
        dbContext.Exercises.Add(new Models.Exercise
        {
            Id = Guid.NewGuid(),
            WorkoutId = myWorkout.Id,
            ExerciseInfoId = exerciseInfo.Id
        });
        await dbContext.SaveChangesAsync();

        var result = await WorkoutHandler.GetWorkoutHistory(HttpContextForUser(userId), new WorkoutRepository(dbContext));

        var ok = result.Should().BeOfType<Ok<GetWorkoutHistoryResponse>>().Subject;
        var summary = ok.Value!.workouts.Should().ContainSingle().Subject;
        summary.id.Should().Be(myWorkout.Id);
        summary.exerciseCount.Should().Be(1);
        summary.exerciseNames.Should().ContainSingle().Which.Should().Be("Squat");
    }

    [Fact]
    public async Task GetWorkoutHistory_LimitsExerciseNamesToThree()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var userId = Guid.NewGuid();
        var workout = new Models.Workout { Id = Guid.NewGuid(), UserId = userId, AddDateTime = DateTime.UtcNow };
        dbContext.Workouts.Add(workout);
        for (var i = 0; i < 5; i++)
        {
            var info = MakeExerciseInfo($"Exercise {i}");
            dbContext.ExerciseInfos.Add(info);
            dbContext.Exercises.Add(new Models.Exercise
            {
                Id = Guid.NewGuid(),
                WorkoutId = workout.Id,
                ExerciseInfoId = info.Id
            });
        }
        await dbContext.SaveChangesAsync();

        var result = await WorkoutHandler.GetWorkoutHistory(HttpContextForUser(userId), new WorkoutRepository(dbContext));

        var ok = result.Should().BeOfType<Ok<GetWorkoutHistoryResponse>>().Subject;
        var summary = ok.Value!.workouts.Should().ContainSingle().Subject;
        summary.exerciseCount.Should().Be(5);
        summary.exerciseNames.Should().HaveCount(3);
    }

    // --- Cancel ---

    [Fact]
    public async Task Cancel_WithExistingWorkout_DeletesItAndReturnsOk()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var workout = new Models.Workout { Id = Guid.NewGuid(), UserId = Guid.NewGuid() };
        dbContext.Workouts.Add(workout);
        await dbContext.SaveChangesAsync();

        var result = await WorkoutHandler.Cancel(workout.Id, new WorkoutRepository(dbContext));

        result.Should().BeOfType<Ok>();
        dbContext.Workouts.Should().BeEmpty();
    }

    [Fact]
    public async Task Cancel_WithUnknownWorkout_ReturnsBadRequest()
    {
        var dbContext = DbContextFactory.CreateInMemory();

        var result = await WorkoutHandler.Cancel(Guid.NewGuid(), new WorkoutRepository(dbContext));

        result.Should().BeOfType<BadRequest>();
    }

    // --- GetWorkoutDetail ---

    [Fact]
    public async Task GetWorkoutDetail_WithUnknownWorkout_ReturnsNotFound()
    {
        var dbContext = DbContextFactory.CreateInMemory();

        var result = await WorkoutHandler.GetWorkoutDetail(
            Guid.NewGuid(),
            ClaimsPrincipalBuilder.WithUserId(Guid.NewGuid()),
            new WorkoutRepository(dbContext),
            new ExerciseRepository(dbContext));

        result.Should().BeOfType<NotFound>();
    }

    [Fact]
    public async Task GetWorkoutDetail_ReturnsWorkoutWithExercisesAndSets()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var userId = Guid.NewGuid();
        var exerciseInfo = MakeExerciseInfo("Overhead Press");
        var workout = new Models.Workout { Id = Guid.NewGuid(), UserId = userId, AddDateTime = DateTime.UtcNow };
        var exercise = new Models.Exercise
        {
            Id = Guid.NewGuid(),
            WorkoutId = workout.Id,
            ExerciseInfoId = exerciseInfo.Id
        };
        exercise.Sets.Add(new Models.Set
        {
            Id = Guid.NewGuid(),
            ExerciseId = exercise.Id,
            Reps = 5,
            Weight = 95m,
            Completed = true,
            Order = 1
        });
        dbContext.ExerciseInfos.Add(exerciseInfo);
        dbContext.Workouts.Add(workout);
        dbContext.Exercises.Add(exercise);
        await dbContext.SaveChangesAsync();

        var result = await WorkoutHandler.GetWorkoutDetail(
            workout.Id,
            ClaimsPrincipalBuilder.WithUserId(userId),
            new WorkoutRepository(dbContext),
            new ExerciseRepository(dbContext));

        var ok = result.Should().BeOfType<Ok<GetWorkoutDetailResponse>>().Subject;
        ok.Value!.workout.id.Should().Be(workout.Id);
        var exerciseDto = ok.Value.workout.exercises.Should().ContainSingle().Subject;
        exerciseDto.exerciseInfo!.name.Should().Be("Overhead Press");
        var setDto = exerciseDto.sets.Should().ContainSingle().Subject;
        setDto.reps.Should().Be(5);
        setDto.weight.Should().Be(95m);
        setDto.completed.Should().BeTrue();
    }

    [Fact]
    public async Task GetWorkoutDetail_PopulatesPreviousExerciseFromUsersLatestExerciseOfSameType()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var userId = Guid.NewGuid();
        var exerciseInfo = MakeExerciseInfo();

        // The "previous exercise" lookup picks the user's most recent exercise
        // for the same exerciseInfoId across all of their workouts.
        var latestWorkout = new Models.Workout
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            AddDateTime = DateTime.UtcNow
        };
        var latestExercise = new Models.Exercise
        {
            Id = Guid.NewGuid(),
            WorkoutId = latestWorkout.Id,
            ExerciseInfoId = exerciseInfo.Id
        };
        latestExercise.Sets.Add(new Models.Set
        {
            Id = Guid.NewGuid(),
            ExerciseId = latestExercise.Id,
            Reps = 12,
            Weight = 60m,
            Order = 1
        });

        var currentWorkout = new Models.Workout { Id = Guid.NewGuid(), UserId = userId, AddDateTime = DateTime.UtcNow.AddDays(-7) };
        var currentExercise = new Models.Exercise
        {
            Id = Guid.NewGuid(),
            WorkoutId = currentWorkout.Id,
            ExerciseInfoId = exerciseInfo.Id
        };

        dbContext.ExerciseInfos.Add(exerciseInfo);
        dbContext.Workouts.AddRange(latestWorkout, currentWorkout);
        dbContext.Exercises.AddRange(latestExercise, currentExercise);
        await dbContext.SaveChangesAsync();

        var result = await WorkoutHandler.GetWorkoutDetail(
            currentWorkout.Id,
            ClaimsPrincipalBuilder.WithUserId(userId),
            new WorkoutRepository(dbContext),
            new ExerciseRepository(dbContext));

        var ok = result.Should().BeOfType<Ok<GetWorkoutDetailResponse>>().Subject;
        var exerciseDto = ok.Value!.workout.exercises.Should().ContainSingle().Subject;
        exerciseDto.previousExercise.Should().NotBeNull();
        exerciseDto.previousExercise!.sets.Should().ContainSingle();
        exerciseDto.previousExercise.sets[0].reps.Should().Be(12);
        exerciseDto.previousExercise.sets[0].weight.Should().Be(60m);
    }
}
