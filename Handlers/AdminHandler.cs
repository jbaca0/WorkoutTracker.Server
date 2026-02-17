using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WorkoutTracker.Server.DataTransferObjects.Common;
using WorkoutTracker.Server.DataTransferObjects.Requests;
using WorkoutTracker.Server.DataTransferObjects.Requests.Admin;
using WorkoutTracker.Server.DataTransferObjects.Responses;
using WorkoutTracker.Server.DataTransferObjects.Responses.Admin;
using WorkoutTracker.Server.DomainObjects;
using WorkoutTracker.Server.Models;
using WorkoutTracker.Server.Models.Repositories;

namespace WorkoutTracker.Server.Handlers;

public static class AdminHandler
{
    public static async Task<IResult> GetUsers(
        UserManager<IdentityUser> userManager)
    {
        var users = await userManager.Users.ToListAsync();
        var userSummaries = new List<UserSummary>();

        foreach (var user in users)
        {
            var roles = await userManager.GetRolesAsync(user);
            userSummaries.Add(new UserSummary
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                Roles = roles.ToList()
            });
        }

        return Results.Ok(new GetUsersResponse { Users = userSummaries });
    }

    public static async Task<IResult> GetRoles(
        RoleManager<IdentityRole> roleManager)
    {
        var roles = await roleManager.Roles
            .Select(r => r.Name!)
            .ToListAsync();

        return Results.Ok(new GetRolesResponse { Roles = roles });
    }

    public static async Task<IResult> GetUserRoles(
        string userId,
        UserManager<IdentityUser> userManager)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return Results.NotFound();
        }

        var roles = await userManager.GetRolesAsync(user);
        return Results.Ok(new GetRolesResponse { Roles = roles.ToList() });
    }

    public static async Task<IResult> CreateUser(
        CreateUserRequest request,
        UserManager<IdentityUser> userManager,
        RoleManager<IdentityRole> roleManager)
    {
        var existingUser = await userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
        {
            return Results.Conflict("A user with this email already exists.");
        }

        var user = new IdentityUser { UserName = request.Email, Email = request.Email };
        var result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            return Results.BadRequest(string.Join(" ", result.Errors.Select(e => e.Description)));
        }

        if (request.Roles.Count > 0)
        {
            foreach (var role in request.Roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    return Results.BadRequest($"Role '{role}' does not exist.");
                }
            }
            await userManager.AddToRolesAsync(user, request.Roles);
        }

        var roles = await userManager.GetRolesAsync(user);
        return Results.Created($"/admin/users/{user.Id}", new UserSummary
        {
            Id = user.Id,
            Email = user.Email,
            Roles = roles.ToList()
        });
    }

    public static async Task<IResult> DeleteUser(
        string userId,
        ClaimsPrincipal currentUser,
        UserManager<IdentityUser> userManager,
        ApplicationDbContext dbContext)
    {
        var currentUserId = currentUser.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == currentUserId)
        {
            return Results.BadRequest("You cannot delete your own account.");
        }

        var user = await userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return Results.NotFound();
        }

        await dbContext.RefreshTokens
            .Where(rt => rt.UserId == userId)
            .ExecuteDeleteAsync();

        var result = await userManager.DeleteAsync(user);
        if (!result.Succeeded)
        {
            return Results.BadRequest(string.Join(" ", result.Errors.Select(e => e.Description)));
        }

        return Results.Ok();
    }

    public static async Task<IResult> UpdateUserRoles(
        string userId,
        UpdateUserRolesRequest request,
        UserManager<IdentityUser> userManager,
        RoleManager<IdentityRole> roleManager)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return Results.NotFound();
        }

        // Validate that all requested roles exist
        foreach (var role in request.Roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                return Results.BadRequest($"Role '{role}' does not exist.");
            }
        }

        var currentRoles = await userManager.GetRolesAsync(user);
        await userManager.RemoveFromRolesAsync(user, currentRoles);
        await userManager.AddToRolesAsync(user, request.Roles);

        return Results.Ok();
    }

    public static async Task<IResult> GetExerciseInfos(
        ExerciseInfoRepository exerciseInfoRepository)
    {
        var exerciseInfos = await exerciseInfoRepository.GetAllAsync();
        var result = new GetExerciseInfosResponse
        {
            exerciseInfos = exerciseInfos.Select(ei => ei.ToData()).ToList()
        };
        return Results.Ok(result);
    }

    public static async Task<IResult> CreateExerciseInfo(
        AddExerciseInfoRequest request,
        ExerciseInfoRepository exerciseInfoRepository)
    {
        var exerciseInfo = new DomainObjects.ExerciseInfo
        {
            Name = request.name,
            PrimaryMuscleGroup = request.primaryMuscleGroup,
            SecondaryMuscleGroups = request.secondaryMuscleGroups
        };
        await exerciseInfoRepository.Add(exerciseInfo);
        return Results.Created($"/admin/exerciseInfos/{exerciseInfo.Id}", exerciseInfo.ToData());
    }

    public static async Task<IResult> UpdateExerciseInfo(
        Guid id,
        UpdateExerciseInfoRequest request,
        ExerciseInfoRepository exerciseInfoRepository)
    {
        var existing = await exerciseInfoRepository.GetByIdAsync(id);
        if (existing == null)
        {
            return Results.NotFound();
        }

        existing.Name = request.Name;
        existing.PrimaryMuscleGroup = request.PrimaryMuscleGroup;
        existing.SecondaryMuscleGroups = request.SecondaryMuscleGroups;

        await exerciseInfoRepository.Update(existing);
        return Results.Ok(existing.ToData());
    }

    public static async Task<IResult> DeleteExerciseInfo(
        Guid id,
        ExerciseInfoRepository exerciseInfoRepository)
    {
        var existing = await exerciseInfoRepository.GetByIdAsync(id);
        if (existing == null)
        {
            return Results.NotFound();
        }

        try
        {
            await exerciseInfoRepository.Delete(id);
            return Results.Ok();
        }
        catch (DbUpdateException)
        {
            return Results.Conflict("Cannot delete this exercise info because it is referenced by existing exercises.");
        }
    }
}
