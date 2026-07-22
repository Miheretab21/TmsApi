using Microsoft.EntityFrameworkCore;
using TmsApi.Application.Interfaces;
using TmsApi.Domain.Entities;

namespace TmsApi.Infrastructure.Persistence;

/// <summary>
/// EF Core implementation of ICourseRepository.
/// Used exclusively by CachedCourseService so the cache layer owns
/// all projection logic without taking a dependency on ICourseService.
/// </summary>
public class CourseRepository(TmsDbContext context) : ICourseRepository
{
    public Task<Course?> GetByCodeAsync(string code, CancellationToken ct) =>
        context.Courses
            .AsNoTracking()
            .Include(c => c.Enrollments)
            .FirstOrDefaultAsync(c => c.Code == code, ct);

    public async Task<IReadOnlyList<Course>> GetAllAsync(CancellationToken ct)
    {
        var courses = await context.Courses
            .AsNoTracking()
            .Include(c => c.Enrollments)
            .OrderBy(c => c.Title)
            .ToListAsync(ct);

        return courses;
    }
}
