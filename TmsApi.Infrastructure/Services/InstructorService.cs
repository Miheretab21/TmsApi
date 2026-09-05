using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TmsApi.Application.Interfaces;
using TmsApi.Domain.Entities;
using TmsApi.Infrastructure.Identity;
using TmsApi.Infrastructure.Persistence;

namespace TmsApi.Infrastructure.Services;

public class InstructorService(
    UserManager<TmsUser> userManager,
    RoleManager<IdentityRole> roleManager,
    TmsDbContext context,
    ILogger<InstructorService> logger) : IInstructorService
{
    public async Task<InstructorResponse> CreateAsync(CreateInstructorRequest request, CancellationToken ct)
    {
        // Check if user already exists
        var existing = await userManager.FindByEmailAsync(request.Email);
        if (existing != null)
        {
            throw new InvalidOperationException($"An instructor with email '{request.Email}' already exists.");
        }

        // Create the user
        var user = new TmsUser
        {
            UserName = request.Email,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Department = request.Department
        };

        var result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to create instructor: {errors}");
        }

        // Ensure Instructor role exists
        if (!await roleManager.RoleExistsAsync("Instructor"))
        {
            await roleManager.CreateAsync(new IdentityRole("Instructor"));
        }

        // Assign Instructor role
        await userManager.AddToRoleAsync(user, "Instructor");

        logger.LogInformation("Created instructor {InstructorId} ({Email})", user.Id, user.Email);

        return new InstructorResponse(user.Id, user.Email!, user.FirstName, user.LastName, user.Department);
    }

    public async Task<InstructorResponse?> GetByIdAsync(string id, CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(id);
        if (user == null) return null;

        var roles = await userManager.GetRolesAsync(user);
        if (!roles.Contains("Instructor")) return null;

        return new InstructorResponse(user.Id, user.Email!, user.FirstName, user.LastName, user.Department);
    }

    public async Task<IReadOnlyList<InstructorResponse>> GetAllAsync(CancellationToken ct)
    {
        var instructorRole = await roleManager.FindByNameAsync("Instructor");
        if (instructorRole == null) return new List<InstructorResponse>();

        var instructors = await context.UserRoles
            .Where(ur => ur.RoleId == instructorRole.Id)
            .Join(
                context.Users,
                ur => ur.UserId,
                u => u.Id,
                (ur, u) => u)
            .Cast<TmsUser>()
            .ToListAsync(ct);

        return instructors
            .Select(u => new InstructorResponse(u.Id, u.Email!, u.FirstName, u.LastName, u.Department))
            .ToList();
    }

    public async Task<bool> UpdateAsync(string id, UpdateInstructorRequest request, CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(id);
        if (user == null) return false;

        user.FirstName = request.FirstName;
        user.LastName = request.LastName;
        user.Department = request.Department;

        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            logger.LogWarning("Failed to update instructor {InstructorId}: {Errors}",
                id, string.Join(", ", result.Errors.Select(e => e.Description)));
            return false;
        }

        logger.LogInformation("Updated instructor {InstructorId}", id);
        return true;
    }

    public async Task<bool> DeleteAsync(string id, CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(id);
        if (user == null) return false;

        var result = await userManager.DeleteAsync(user);
        if (!result.Succeeded)
        {
            logger.LogWarning("Failed to delete instructor {InstructorId}: {Errors}",
                id, string.Join(", ", result.Errors.Select(e => e.Description)));
            return false;
        }

        logger.LogInformation("Deleted instructor {InstructorId}", id);
        return true;
    }
}
