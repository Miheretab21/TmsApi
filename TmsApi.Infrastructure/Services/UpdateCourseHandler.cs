using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TmsApi.Application.Courses.Commands;
using TmsApi.Application.Interfaces;
using TmsApi.Infrastructure.Persistence;

namespace TmsApi.Infrastructure.Services;

/// <summary>
/// MediatR handler that updates a course and then invalidates the HybridCache
/// for all course entries (Step 6: invalidate on writes).
/// </summary>
public class UpdateCourseHandler(
    TmsDbContext context,
    ICachedCourseService cachedService,
    ILogger<UpdateCourseHandler> logger)
    : IRequestHandler<UpdateCourseCommand, bool>
{
    public async Task<bool> Handle(UpdateCourseCommand command, CancellationToken ct)
    {
        var course = await context.Courses
            .FirstOrDefaultAsync(c => c.Id == command.Id, ct);

        if (course is null)
            return false;

        course.Title       = command.Title;
        course.MaxCapacity = command.MaxCapacity;

        await context.SaveChangesAsync(ct);
        logger.LogInformation("Updated course {CourseId}", command.Id);

        // Step 6 – invalidate the courses cache after every write
        await cachedService.InvalidateCourseCacheAsync(ct);

        return true;
    }
}
