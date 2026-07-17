using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TmsApi.Infrastructure.Persistence;

namespace TmsApi.Api.Controllers;

[ApiController]
[Route("api/reports")]
public class ReportsController(TmsDbContext context) : ControllerBase
{
    [HttpGet("active-high-gpa-count")]
    public async Task<IActionResult> GetActiveHighGpaCount()
    {
        var count = await context.Students
            .Where(s => s.IsActive && s.GPA >= 3.0m)
            .CountAsync();
        return Ok(new { ActiveStudentsWithGpaAbove3 = count });
    }

    [HttpGet("courses-by-enrollment")]
    public async Task<IActionResult> GetCoursesByEnrollment()
    {
        var list = await context.Courses
            .Select(c => new { c.Title, EnrollmentCount = c.Enrollments.Count })
            .OrderByDescending(x => x.EnrollmentCount)
            .ToListAsync();
        return Ok(list);
    }

    [HttpGet("average-gpa-per-course")]
    public async Task<IActionResult> GetAverageGpaPerCourse()
    {
        var list = await context.Enrollments
            .GroupBy(e => e.Course.Title)
            .Select(g => new { Course = g.Key, AverageGPA = g.Average(e => e.Student.GPA) })
            .ToListAsync();
        return Ok(list);
    }

    [HttpGet("unenrolled-students-subquery")]
    public async Task<IActionResult> GetUnenrolledStudentsSubquery()
    {
        var list = await context.Students
            .Where(s => !s.Enrollments.Any())
            .Select(s => s.Name)
            .ToListAsync();
        return Ok(list);
    }

    [HttpGet("unenrolled-students-leftjoin")]
    public async Task<IActionResult> GetUnenrolledStudentsLeftJoin()
    {
        var list = await context.Students
            .LeftJoin(context.Enrollments, s => s.Id, e => e.StudentId, (s, e) => new { s, e })
            .Where(x => x.e == null)
            .Select(x => x.s.Name)
            .ToListAsync();
        return Ok(list);
    }

    [HttpGet("students")]
    public async Task<IActionResult> GetStudentsPaged(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var students = await context.Students
            .OrderBy(s => s.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new { s.Id, s.Name, s.RegistrationNumber, s.GPA, s.IsActive })
            .ToListAsync(ct);

        return Ok(students);
    }

    [HttpGet("top-courses")]
    public async Task<IActionResult> GetTopCoursesByEnrollment(CancellationToken ct)
    {
        var top5 = await context.Enrollments
            .GroupBy(e => e.Course.Title)
            .Select(g => new { Course = g.Key, EnrollmentCount = g.Count() })
            .OrderByDescending(x => x.EnrollmentCount)
            .Take(5)
            .ToListAsync(ct);

        return Ok(top5);
    }

    [HttpGet("enrollment-counts-n-plus-1")]
    public async Task<IActionResult> GetEnrollmentCountsNPlusOne(CancellationToken ct)
    {
        var students = await context.Students
            .AsNoTracking()
            .ToListAsync(ct);

        var result = new List<object>();

        foreach (var s in students)
        {
            var count = await context.Enrollments
                .AsNoTracking()
                .CountAsync(e => e.StudentId == s.Id, ct);

            result.Add(new { s.Name, EnrollmentCount = count });
        }

        return Ok(result);
    }

    [HttpGet("enrollment-counts-shaped")]
    public async Task<IActionResult> GetEnrollmentCountsShaped(CancellationToken ct)
    {
        var report = await context.Students
            .AsNoTracking()
            .Select(s => new
            {
                s.Name,
                EnrollmentCount = s.Enrollments.Count
            })
            .ToListAsync(ct);

        return Ok(report);
    }
}
