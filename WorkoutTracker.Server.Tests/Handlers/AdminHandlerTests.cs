using FluentAssertions;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using WorkoutTracker.Server.DataTransferObjects.Common;
using WorkoutTracker.Server.DataTransferObjects.Requests;
using WorkoutTracker.Server.DataTransferObjects.Requests.Admin;
using WorkoutTracker.Server.DataTransferObjects.Responses;
using WorkoutTracker.Server.DataTransferObjects.Responses.Admin;
using WorkoutTracker.Server.Enums;
using WorkoutTracker.Server.Handlers;
using WorkoutTracker.Server.Models.Repositories;
using WorkoutTracker.Server.Tests.Helpers;
using Xunit;

namespace WorkoutTracker.Server.Tests.Handlers;

public class AdminHandlerTests
{
    private const string Password = "Password1!";

    private static async Task<IdentityUser> CreateUserAsync(
        UserManager<IdentityUser> userManager,
        string email,
        params string[] roles)
    {
        var user = new IdentityUser { UserName = email, Email = email };
        var result = await userManager.CreateAsync(user, Password);
        result.Succeeded.Should().BeTrue();
        if (roles.Length > 0)
        {
            (await userManager.AddToRolesAsync(user, roles)).Succeeded.Should().BeTrue();
        }
        return user;
    }

    private static async Task CreateRoleAsync(RoleManager<IdentityRole> roleManager, string role)
    {
        (await roleManager.CreateAsync(new IdentityRole(role))).Succeeded.Should().BeTrue();
    }

    // --- GetUsers ---

    [Fact]
    public async Task GetUsers_ReturnsAllUsersWithTheirRoles()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var userManager = IdentityFactory.CreateUserManager(dbContext);
        var roleManager = IdentityFactory.CreateRoleManager(dbContext);
        await CreateRoleAsync(roleManager, "Admin");
        var admin = await CreateUserAsync(userManager, "admin@example.com", "Admin");
        var regular = await CreateUserAsync(userManager, "user@example.com");

        var result = await AdminHandler.GetUsers(userManager);

        var ok = result.Should().BeOfType<Ok<GetUsersResponse>>().Subject;
        ok.Value!.Users.Should().HaveCount(2);
        ok.Value.Users.Single(u => u.Id == admin.Id).Roles.Should().BeEquivalentTo("Admin");
        ok.Value.Users.Single(u => u.Id == regular.Id).Roles.Should().BeEmpty();
    }

    // --- GetRoles ---

    [Fact]
    public async Task GetRoles_ReturnsAllRoleNames()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var roleManager = IdentityFactory.CreateRoleManager(dbContext);
        await CreateRoleAsync(roleManager, "Admin");
        await CreateRoleAsync(roleManager, "User");

        var result = await AdminHandler.GetRoles(roleManager);

        var ok = result.Should().BeOfType<Ok<GetRolesResponse>>().Subject;
        ok.Value!.Roles.Should().BeEquivalentTo("Admin", "User");
    }

    // --- GetUserRoles ---

    [Fact]
    public async Task GetUserRoles_WithUnknownUser_ReturnsNotFound()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var userManager = IdentityFactory.CreateUserManager(dbContext);

        var result = await AdminHandler.GetUserRoles(Guid.NewGuid().ToString(), userManager);

        result.Should().BeOfType<NotFound>();
    }

    [Fact]
    public async Task GetUserRoles_WithExistingUser_ReturnsTheirRoles()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var userManager = IdentityFactory.CreateUserManager(dbContext);
        var roleManager = IdentityFactory.CreateRoleManager(dbContext);
        await CreateRoleAsync(roleManager, "Admin");
        var user = await CreateUserAsync(userManager, "admin@example.com", "Admin");

        var result = await AdminHandler.GetUserRoles(user.Id, userManager);

        var ok = result.Should().BeOfType<Ok<GetRolesResponse>>().Subject;
        ok.Value!.Roles.Should().BeEquivalentTo("Admin");
    }

    // --- CreateUser ---

    [Fact]
    public async Task CreateUser_WithDuplicateEmail_ReturnsConflict()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var userManager = IdentityFactory.CreateUserManager(dbContext);
        var roleManager = IdentityFactory.CreateRoleManager(dbContext);
        await CreateUserAsync(userManager, "existing@example.com");

        var request = new CreateUserRequest { Email = "existing@example.com", Password = Password };
        var result = await AdminHandler.CreateUser(request, userManager, roleManager);

        result.Should().BeOfType<Conflict<string>>();
    }

    [Fact]
    public async Task CreateUser_WithWeakPassword_ReturnsBadRequest()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var userManager = IdentityFactory.CreateUserManager(dbContext);
        var roleManager = IdentityFactory.CreateRoleManager(dbContext);

        var request = new CreateUserRequest { Email = "new@example.com", Password = "weak" };
        var result = await AdminHandler.CreateUser(request, userManager, roleManager);

        result.Should().BeOfType<BadRequest<string>>();
    }

    [Fact]
    public async Task CreateUser_WithNonExistentRole_ReturnsBadRequest()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var userManager = IdentityFactory.CreateUserManager(dbContext);
        var roleManager = IdentityFactory.CreateRoleManager(dbContext);

        var request = new CreateUserRequest
        {
            Email = "new@example.com",
            Password = Password,
            Roles = ["DoesNotExist"]
        };
        var result = await AdminHandler.CreateUser(request, userManager, roleManager);

        var badRequest = result.Should().BeOfType<BadRequest<string>>().Subject;
        badRequest.Value.Should().Contain("DoesNotExist");
    }

    [Fact]
    public async Task CreateUser_WithValidRequest_CreatesUserWithRolesAndReturnsCreated()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var userManager = IdentityFactory.CreateUserManager(dbContext);
        var roleManager = IdentityFactory.CreateRoleManager(dbContext);
        await CreateRoleAsync(roleManager, "Admin");

        var request = new CreateUserRequest
        {
            Email = "new@example.com",
            Password = Password,
            Roles = ["Admin"]
        };
        var result = await AdminHandler.CreateUser(request, userManager, roleManager);

        var created = result.Should().BeOfType<Created<UserSummary>>().Subject;
        created.Value!.Email.Should().Be("new@example.com");
        created.Value.Roles.Should().BeEquivalentTo("Admin");
        created.Location.Should().Be($"/admin/users/{created.Value.Id}");

        var storedUser = await userManager.FindByEmailAsync("new@example.com");
        storedUser.Should().NotBeNull();
    }

    // --- DeleteUser ---
    // Note: the success path calls ExecuteDeleteAsync, which the EF InMemory
    // provider does not support, so only the guard paths are unit tested here.

    [Fact]
    public async Task DeleteUser_WhenDeletingOwnAccount_ReturnsBadRequest()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var userManager = IdentityFactory.CreateUserManager(dbContext);
        var userId = Guid.NewGuid();

        var result = await AdminHandler.DeleteUser(
            userId.ToString(),
            ClaimsPrincipalBuilder.WithUserId(userId),
            userManager,
            dbContext);

        result.Should().BeOfType<BadRequest<string>>();
    }

    [Fact]
    public async Task DeleteUser_WithUnknownUser_ReturnsNotFound()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var userManager = IdentityFactory.CreateUserManager(dbContext);

        var result = await AdminHandler.DeleteUser(
            Guid.NewGuid().ToString(),
            ClaimsPrincipalBuilder.WithUserId(Guid.NewGuid()),
            userManager,
            dbContext);

        result.Should().BeOfType<NotFound>();
    }

    // --- UpdateUserRoles ---

    [Fact]
    public async Task UpdateUserRoles_WithUnknownUser_ReturnsNotFound()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var userManager = IdentityFactory.CreateUserManager(dbContext);
        var roleManager = IdentityFactory.CreateRoleManager(dbContext);

        var result = await AdminHandler.UpdateUserRoles(
            Guid.NewGuid().ToString(),
            new UpdateUserRolesRequest { Roles = ["Admin"] },
            userManager,
            roleManager);

        result.Should().BeOfType<NotFound>();
    }

    [Fact]
    public async Task UpdateUserRoles_WithNonExistentRole_ReturnsBadRequest()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var userManager = IdentityFactory.CreateUserManager(dbContext);
        var roleManager = IdentityFactory.CreateRoleManager(dbContext);
        var user = await CreateUserAsync(userManager, "user@example.com");

        var result = await AdminHandler.UpdateUserRoles(
            user.Id,
            new UpdateUserRolesRequest { Roles = ["Ghost"] },
            userManager,
            roleManager);

        var badRequest = result.Should().BeOfType<BadRequest<string>>().Subject;
        badRequest.Value.Should().Contain("Ghost");
    }

    [Fact]
    public async Task UpdateUserRoles_WithValidRoles_ReplacesExistingRoles()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var userManager = IdentityFactory.CreateUserManager(dbContext);
        var roleManager = IdentityFactory.CreateRoleManager(dbContext);
        await CreateRoleAsync(roleManager, "Admin");
        await CreateRoleAsync(roleManager, "User");
        var user = await CreateUserAsync(userManager, "user@example.com", "User");

        var result = await AdminHandler.UpdateUserRoles(
            user.Id,
            new UpdateUserRolesRequest { Roles = ["Admin"] },
            userManager,
            roleManager);

        result.Should().BeOfType<Ok>();
        (await userManager.GetRolesAsync(user)).Should().BeEquivalentTo("Admin");
    }

    // --- GetExerciseInfos ---

    [Fact]
    public async Task GetExerciseInfos_ReturnsAllExerciseInfos()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        dbContext.ExerciseInfos.Add(new Models.ExerciseInfo { Id = Guid.NewGuid(), Name = "Bench Press", SecondaryMuscleGroupsJson = "[]" });
        dbContext.ExerciseInfos.Add(new Models.ExerciseInfo { Id = Guid.NewGuid(), Name = "Squat", SecondaryMuscleGroupsJson = "[]" });
        await dbContext.SaveChangesAsync();

        var result = await AdminHandler.GetExerciseInfos(new ExerciseInfoRepository(dbContext));

        var ok = result.Should().BeOfType<Ok<GetExerciseInfosResponse>>().Subject;
        ok.Value!.exerciseInfos.Should().HaveCount(2);
        ok.Value.exerciseInfos.Select(ei => ei.name).Should().BeEquivalentTo("Bench Press", "Squat");
    }

    // --- CreateExerciseInfo ---

    [Fact]
    public async Task CreateExerciseInfo_PersistsExerciseInfoAndReturnsCreated()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var request = new AddExerciseInfoRequest
        {
            name = "Pull Up",
            primaryMuscleGroup = MuscleGroup.Back,
            secondaryMuscleGroups = [MuscleGroup.Biceps]
        };

        var result = await AdminHandler.CreateExerciseInfo(request, new ExerciseInfoRepository(dbContext));

        var created = result.Should().BeOfType<Created<DataTransferObjects.Common.ExerciseInfo>>().Subject;
        created.Value!.name.Should().Be("Pull Up");
        created.Value.primaryMuscleGroup.Should().Be(MuscleGroup.Back);
        dbContext.ExerciseInfos.Should().ContainSingle(ei => ei.Name == "Pull Up");
    }

    // --- UpdateExerciseInfo ---

    [Fact]
    public async Task UpdateExerciseInfo_WithUnknownId_ReturnsNotFound()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var request = new UpdateExerciseInfoRequest { Name = "Renamed" };

        var result = await AdminHandler.UpdateExerciseInfo(Guid.NewGuid(), request, new ExerciseInfoRepository(dbContext));

        result.Should().BeOfType<NotFound>();
    }

    [Fact]
    public async Task UpdateExerciseInfo_WithExistingId_UpdatesAndReturnsOk()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var id = Guid.NewGuid();
        dbContext.ExerciseInfos.Add(new Models.ExerciseInfo
        {
            Id = id,
            Name = "Old Name",
            PrimaryMuscleGroup = MuscleGroup.Chest,
            SecondaryMuscleGroupsJson = "[]"
        });
        await dbContext.SaveChangesAsync();

        var request = new UpdateExerciseInfoRequest
        {
            Name = "New Name",
            PrimaryMuscleGroup = MuscleGroup.Back,
            SecondaryMuscleGroups = [MuscleGroup.Biceps]
        };
        var result = await AdminHandler.UpdateExerciseInfo(id, request, new ExerciseInfoRepository(dbContext));

        var ok = result.Should().BeOfType<Ok<DataTransferObjects.Common.ExerciseInfo>>().Subject;
        ok.Value!.name.Should().Be("New Name");
        ok.Value.primaryMuscleGroup.Should().Be(MuscleGroup.Back);

        var stored = dbContext.ExerciseInfos.Single(ei => ei.Id == id);
        stored.Name.Should().Be("New Name");
        stored.PrimaryMuscleGroup.Should().Be(MuscleGroup.Back);
    }

    // --- DeleteExerciseInfo ---

    [Fact]
    public async Task DeleteExerciseInfo_WithUnknownId_ReturnsNotFound()
    {
        var dbContext = DbContextFactory.CreateInMemory();

        var result = await AdminHandler.DeleteExerciseInfo(Guid.NewGuid(), new ExerciseInfoRepository(dbContext));

        result.Should().BeOfType<NotFound>();
    }

    [Fact]
    public async Task DeleteExerciseInfo_WithExistingId_DeletesAndReturnsOk()
    {
        var dbContext = DbContextFactory.CreateInMemory();
        var id = Guid.NewGuid();
        dbContext.ExerciseInfos.Add(new Models.ExerciseInfo { Id = id, Name = "Doomed", SecondaryMuscleGroupsJson = "[]" });
        await dbContext.SaveChangesAsync();

        var result = await AdminHandler.DeleteExerciseInfo(id, new ExerciseInfoRepository(dbContext));

        result.Should().BeOfType<Ok>();
        dbContext.ExerciseInfos.Should().BeEmpty();
    }
}
