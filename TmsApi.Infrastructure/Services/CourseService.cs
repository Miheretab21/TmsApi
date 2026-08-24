using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TmsApi.Application.DTOs;
using TmsApi.Application.Interfaces;
using TmsApi.Domain.Entities;
using TmsApi.Infrastructure.Persistence;

namespace TmsApi.Infrastructure.Services;

public class CourseService(
    TmsDbContext context,
    ICachedCourseService cachedCourseService,
    ILogger<CourseService> logger) : ICourseService
{
    public Task<CourseResponseDto?> GetByIdAsync(int id, CancellationToken ct) =>
        context.Courses
            .AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new CourseResponseDto(
                c.Id, c.Code, c.Title, c.MaxCapacity, c.Enrollments.Count))
            .FirstOrDefaultAsync(ct);

    /// <summary>Returns the tracked Course entity (including InstructorId) for authorization checks.</summary>
    public Task<Course?> GetEntityByIdAsync(int id, CancellationToken ct) =>
        context.Courses.FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<CourseResponseDto> CreateAsync(CreateCourseRequest request, CancellationToken ct)
    {
        var course = new Course
        {
            Code        = request.Code,
            Title       = request.Title,
            MaxCapacity = request.MaxCapacity
        };
        context.Courses.Add(course);
        await context.SaveChangesAsync(ct);
        logger.LogInformation("Created course {CourseId} ({Code})", course.Id, course.Code);
        await cachedCourseService.InvalidateCourseCacheAsync(ct);
        return (await GetByIdAsync(course.Id, ct))!;
    }

    public Task<bool> CodeExistsAsync(string code, CancellationToken ct) =>
        context.Courses.AsNoTracking().AnyAsync(c => c.Code == code, ct);

    public async Task<PagedResponse<CourseResponseDto>> GetCoursesAsync(
        PagedRequest request, CancellationToken ct)
    {
        IQueryable<Course> query = context.Courses.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Search))
            query = query.Where(c =>
                EF.Functions.ILike(c.Title, $"%{request.Search}%") ||
                EF.Functions.ILike(c.Code,  $"%{request.Search}%"));

        var totalCount = await query.CountAsync(ct);

        IQueryable<Course> sortedQuery = request.OrderBy switch
        {
            "Code"        => request.Descending ? query.OrderByDescending(c => c.Code)        : query.OrderBy(c => c.Code),
            "MaxCapacity" => request.Descending ? query.OrderByDescending(c => c.MaxCapacity) : query.OrderBy(c => c.MaxCapacity),
            _             => request.Descending ? query.OrderByDescending(c => c.Title)       : query.OrderBy(c => c.Title),
        };

        var items = await sortedQuery
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(c => new CourseResponseDto(c.Id, c.Code, c.Title, c.MaxCapacity, c.Enrollments.Count))
            .ToListAsync(ct);

        return new PagedResponse<CourseResponseDto>
        {
            Items      = items,
            TotalCount = totalCount,
            Page       = request.Page,
            PageSize   = request.PageSize
        };
    }

    public Task<Course?> GetByCodeAsync(string code, CancellationToken ct = default) =>
        context.Courses
            .Include(c => c.Enrollments)
            .FirstOrDefaultAsync(c => c.Code == code, ct);

    public async Task<bool> DeleteAsync(int id, CancellationToken ct)
    {
        var course = await context.Courses
            .Include(c => c.Enrollments)
            .FirstOrDefaultAsync(c => c.Id == id, ct);

        if (course is null) return false;

        // Signal the caller to return 409 — active enrollments block deletion
        if (course.Enrollments.Count > 0) return false;

        context.Courses.Remove(course);
        await context.SaveChangesAsync(ct);
        await cachedCourseService.InvalidateCourseCacheAsync(ct);
        return true;
    }

    public async Task<bool> UpdateAsync(int id, string title, int maxCapacity, CancellationToken ct)
    {
        var course = await context.Courses.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (course is null) return false;

        course.Title       = title;
        course.MaxCapacity = maxCapacity;

        await context.SaveChangesAsync(ct);
        await cachedCourseService.InvalidateCourseCacheAsync(ct);

        logger.LogInformation("Updated course {CourseId}: Title={Title}, MaxCapacity={MaxCapacity}",
            id, title, maxCapacity);
        return true;
    }
}
