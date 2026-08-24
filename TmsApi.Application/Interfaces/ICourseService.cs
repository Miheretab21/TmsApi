using TmsApi.Application.DTOs;
using TmsApi.Domain.Entities;

namespace TmsApi.Application.Interfaces;

public interface ICourseService
{
    Task<CourseResponseDto?> GetByIdAsync(int id, CancellationToken ct);

    /// <summary>Returns the full Course entity (with InstructorId) for resource-based authorization checks.</summary>
    Task<Course?> GetEntityByIdAsync(int id, CancellationToken ct);

    Task<CourseResponseDto> CreateAsync(CreateCourseRequest request, CancellationToken ct);
    Task<bool> CodeExistsAsync(string code, CancellationToken ct);
    Task<PagedResponse<CourseResponseDto>> GetCoursesAsync(PagedRequest request, CancellationToken ct);
    Task<Course?> GetByCodeAsync(string code, CancellationToken ct = default);
    Task<bool> DeleteAsync(int id, CancellationToken ct);

    /// <summary>Updates Title and MaxCapacity of a course by ID. Returns false when the course is not found.</summary>
    Task<bool> UpdateAsync(int id, string title, int maxCapacity, CancellationToken ct);
}
