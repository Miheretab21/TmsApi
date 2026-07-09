using Microsoft.EntityFrameworkCore;
using TmsApi.Data;
using TmsApi.Dtos;
using TmsApi.Entities;

namespace TmsApi.Services;

public class CourseService(TmsDbContext context, ILogger<CourseService> logger) : ICourseService
{
    public Task<CourseResponseDto?> GetByIdAsync(int id, CancellationToken ct) =>
        context.Courses
            .AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new CourseResponseDto(
                c.Id, c.Code, c.Title, c.MaxCapacity, c.Enrollments.Count))
            .FirstOrDefaultAsync(ct);

    public async Task<CourseResponseDto> CreateAsync(CreateCourseRequest request, CancellationToken ct)
    {
        var course = new Course
        {
            Code = request.Code,
            Title = request.Title,
            MaxCapacity = request.MaxCapacity
        };
        context.Courses.Add(course);
        await context.SaveChangesAsync(ct);
        logger.LogInformation("Created course {CourseId} ({Code})", course.Id, course.Code);
        return (await GetByIdAsync(course.Id, ct))!;
    }

    public Task<bool> CodeExistsAsync(string code, CancellationToken ct) =>
        context.Courses.AsNoTracking().AnyAsync(c => c.Code == code, ct);

    public async Task<PagedResponse<CourseResponseDto>> GetCoursesAsync(
        PagedRequest request, CancellationToken ct)
    {
        // TODO 1: Start with a no-tracking IQueryable<Course>
        IQueryable<Course> query = context.Courses.AsNoTracking();

        // TODO 2: If request.Search has a value, filter by Title or Code (case-insensitive)
        if (!string.IsNullOrWhiteSpace(request.Search))
            query = query.Where(c =>
                EF.Functions.ILike(c.Title, $"%{request.Search}%") ||
                EF.Functions.ILike(c.Code,  $"%{request.Search}%"));

        // TODO 3: Count BEFORE paging — produces one SELECT COUNT(*)
        var totalCount = await query.CountAsync(ct);

        // TODO 4: Apply OrderBy — whitelist allowed columns, fall back to Title
        IQueryable<Course> sortedQuery = request.OrderBy switch
        {
            "Code"        => request.Descending ? query.OrderByDescending(c => c.Code)        : query.OrderBy(c => c.Code),
            "MaxCapacity" => request.Descending ? query.OrderByDescending(c => c.MaxCapacity) : query.OrderBy(c => c.MaxCapacity),
            _             => request.Descending ? query.OrderByDescending(c => c.Title)       : query.OrderBy(c => c.Title),
        };

        // TODO 5: Skip/Take then Select projection inside the IQueryable chain
        var items = await sortedQuery
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(c => new CourseResponseDto(c.Id, c.Code, c.Title, c.MaxCapacity, c.Enrollments.Count))
            .ToListAsync(ct);

        // TODO 6: Return PagedResponse
        return new PagedResponse<CourseResponseDto>
        {
            Items      = items,
            TotalCount = totalCount,
            Page       = request.Page,
            PageSize   = request.PageSize
        };
    }
}
