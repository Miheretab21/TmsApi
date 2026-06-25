using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TmsApi.Data;

namespace TmsApi.Controllers;

[ApiController]
[Route("api/reports")]
public class ReportsController(TmsDbContext context) : ControllerBase
{
    // Query 1: How many active students have GPA >= 3.0?
    // SQL: SELECT COUNT(*)::int FROM "Students" AS s WHERE s."IsActive" AND s."GPA" >= 3.0
    [HttpGet("active-high-gpa-count")]
    public async Task<IActionResult> GetActiveHighGpaCount()
    {
        var count = await context.Students
            .Where(s => s.IsActive && s.GPA >= 3.0m)
            .CountAsync();
        return Ok(new { ActiveStudentsWithGpaAbove3 = count });
    }

    // Query 2: Which courses have the most enrollments, sorted descending?
    // SQL: ORDER BY and COUNT calculated in the database
    [HttpGet("courses-by-enrollment")]
    public async Task<IActionResult> GetCoursesByEnrollment()
    {
        var list = await context.Courses
            .Select(c => new { c.Title, EnrollmentCount = c.Enrollments.Count })
            .OrderByDescending(x => x.EnrollmentCount)
            .ToListAsync();
        return Ok(list);
    }

    // Query 3: What is the average GPA per course?
    // SQL: GROUP BY with AVG aggregation in the database
    [HttpGet("average-gpa-per-course")]
    public async Task<IActionResult> GetAverageGpaPerCourse()
    {
        var list = await context.Enrollments
            .GroupBy(e => e.Course.Title)
            .Select(g => new { Course = g.Key, AverageGPA = g.Average(e => e.Student.GPA) })
            .ToListAsync();
        return Ok(list);
    }

    // Query 4A: Which students have zero enrollments? (subquery approach)
    // SQL: NOT EXISTS (SELECT 1 FROM "Enrollments" WHERE ...)
    [HttpGet("unenrolled-students-subquery")]
    public async Task<IActionResult> GetUnenrolledStudentsSubquery()
    {
        var list = await context.Students
            .Where(s => !s.Enrollments.Any())
            .Select(s => s.Name)
            .ToListAsync();
        return Ok(list);
    }

    // Query 4B: Which students have zero enrollments? (EF Core 10 LeftJoin approach)
    // SQL: LEFT JOIN "Enrollments" ... WHERE e."Id" IS NULL
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

    // Exercise 3 Todo 1: Paged list of students — stable sort by name, page size 20
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

    // Exercise 3 Todo 2: Top 5 courses by enrollment count
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

    // -------------------------------------------------------------------------
    // Exercise 7 — Part A: Intentional N+1 (for learning)
    // -------------------------------------------------------------------------
    // Produces 1 query to load all students, then 1 extra query PER student to
    // count their enrollments — total: 1 + N SQL statements in the log.
    // Watch the console: you will see a separate COUNT query for every student.
    [HttpGet("enrollment-counts-n-plus-1")]
    public async Task<IActionResult> GetEnrollmentCountsNPlusOne(CancellationToken ct)
    {
        // Query 1: fetch all students (1 SQL statement)
        var students = await context.Students
            .AsNoTracking()
            .ToListAsync(ct);

        var result = new List<object>();

        foreach (var s in students)
        {
            // Query 2..N+1: one COUNT per student — this is the N+1 problem
            var count = await context.Enrollments
                .AsNoTracking()
                .CountAsync(e => e.StudentId == s.Id, ct);

            result.Add(new { s.Name, EnrollmentCount = count });
        }

        return Ok(result);
    }

    // -------------------------------------------------------------------------
    // Exercise 7 — Part B: Fix with a shaped single query
    // -------------------------------------------------------------------------
    // EF Core translates s.Enrollments.Count into a SQL subquery inside the
    // SELECT, so the entire result comes back in ONE SQL statement instead of
    // 1 + N.  Compare the console output with the endpoint above.
    [HttpGet("enrollment-counts-shaped")]
    public async Task<IActionResult> GetEnrollmentCountsShaped(CancellationToken ct)
    {
        // Single query: EF emits SELECT ..., (SELECT COUNT(*) FROM "Enrollments"
        // WHERE "StudentId" = s."Id") AS "EnrollmentCount" FROM "Students" AS s
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
