using MediatR;
using Microsoft.EntityFrameworkCore;
using TmsApi.Application.Courses.Queries;
using TmsApi.Application.DTOs;
using TmsApi.Infrastructure.Persistence;

namespace TmsApi.Infrastructure.Services;

public class SearchCoursesHandler(TmsDbContext context) : IRequestHandler<SearchCoursesQuery, List<CourseDto>>
{
    public async Task<List<CourseDto>> Handle(SearchCoursesQuery request, CancellationToken ct)
    {
        IQueryable<TmsApi.Domain.Entities.Course> query = context.Courses.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Term))
        {
            query = query.Where(c =>
                EF.Functions.ILike(c.Title, $"%{request.Term}%") ||
                EF.Functions.ILike(c.Code, $"%{request.Term}%"));
        }

        var courses = await query
            .OrderBy(c => c.Title)
            .Select(c => new CourseDto(
                c.Id,
                c.Title,
                c.Code,
                c.MaxCapacity,
                c.Enrollments.Count))
            .ToListAsync(ct);

        return courses;
    }
}
