using TmsApi.Domain.Entities;

namespace TmsApi.Application.Interfaces;

/// <summary>
/// Raw data-access contract used by the cache layer.
/// Returns domain entities so CachedCourseService can project them
/// into CourseDto without depending on ICourseService.
/// </summary>
public interface ICourseRepository
{
    Task<Course?> GetByCodeAsync(string code, CancellationToken ct);
    Task<IReadOnlyList<Course>> GetAllAsync(CancellationToken ct);
}
