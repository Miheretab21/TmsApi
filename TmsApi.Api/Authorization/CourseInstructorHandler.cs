using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using TmsApi.Domain.Entities;

namespace TmsApi.Api.Authorization;

/// <summary>
/// Resource-based authorization handler for the CourseInstructorRequirement.
/// Succeeds when:
///   - The current user is an Admin, OR
///   - The current user is an Instructor AND owns the course (InstructorId == UserId).
/// </summary>
public class CourseInstructorHandler
    : AuthorizationHandler<CourseInstructorRequirement, Course>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        CourseInstructorRequirement requirement,
        Course resource)
    {
        var userId      = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        var isInstructor = context.User.IsInRole("Instructor");
        var isAdmin      = context.User.IsInRole("Admin");

        if (isAdmin || (isInstructor && resource.InstructorId == userId))
            context.Succeed(requirement);

        // Gracefully return without calling Succeed() — avoids NullRef on failed checks
        return Task.CompletedTask;
    }
}
