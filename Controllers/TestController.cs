using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TmsApi.Data;

namespace TmsApi.Controllers;

[ApiController]
[Route("api/test")]
public class TestController(TmsDbContext context) : ControllerBase
{
    // Non-translatable helper method
    private static bool IsHonorRoll(decimal gpa) => gpa >= 3.5m;

    [HttpGet("translation-fail")]
    public IActionResult TestTranslationFail()
    {
        Console.WriteLine("\n>>> STEP 1: Running non-translatable query...");
        try
        {
            var students = context.Students
                .Where(s => IsHonorRoll(s.GPA)) // EF Core does not know how to map this method to SQL
                .ToList();
            return Ok(students);
        }
        catch (Exception ex)
        {
            Console.WriteLine($">>> EXCEPTION CAUGHT: {ex.Message}\n");
            return BadRequest(new { Message = ex.Message });
        }
    }

    [HttpGet("translation-fix-server")]
    public IActionResult TestTranslationFixServer()
    {
        // Resolution 1: Server-side evaluation (preferred) — inline logic EF Core can translate
        var students = context.Students
            .Where(s => s.GPA >= 3.5m)
            .ToList();
        return Ok(students);
    }

    [HttpGet("translation-fix-client")]
    public IActionResult TestTranslationFixClient()
    {
        // Resolution 2: Client-side evaluation — pulls ALL rows into memory first (watch the SQL log)
        var students = context.Students
            .AsEnumerable()                     // Pulls all rows into application RAM
            .Where(s => IsHonorRoll(s.GPA))
            .ToList();
        return Ok(students);
    }

    [HttpGet("deferred")]
    public IActionResult TestDeferred()
    {
        Console.WriteLine("\n>>> STEP 1: Building the query object (no database contact)...");
        var query = context.Students.Where(s => s.GPA >= 3.0m);

        Console.WriteLine(">>> STEP 2: Appending a sorting clause...");
        var orderedQuery = query.OrderBy(s => s.Name);

        Console.WriteLine(">>> STEP 3: Materializing query into a C# List...");
        var results = orderedQuery.ToList(); // Execution is triggered here

        Console.WriteLine(">>> STEP 4: Materialization finished. List populated.\n");

        return Ok(results);
    }

    // Query 1: How many active students have GPA >= 3.0?
    [HttpGet("registrar/active-high-gpa-count")]
    public async Task<IActionResult> GetActiveHighGpaCount()
    {
        var count = await context.Students
            .Where(s => s.IsActive && s.GPA >= 3.0m)
            .CountAsync();
        // SQL: SELECT COUNT(*)::int4 FROM "Students" AS s WHERE s."IsActive" AND s."GPA" >= 3.0
        return Ok(new { ActiveStudentsWithGpaAbove3 = count });
    }

    // Query 2: Which courses have the most enrollments, sorted descending?
    [HttpGet("registrar/courses-by-enrollment")]
    public async Task<IActionResult> GetCoursesByEnrollment()
    {
        var list = await context.Courses
            .Select(c => new { c.Title, EnrollmentCount = c.Enrollments.Count })
            .OrderByDescending(x => x.EnrollmentCount)
            .ToListAsync();
        // SQL: ORDER BY and COUNT happening in the database
        return Ok(list);
    }

    // Query 3: What is the average GPA per course?
    [HttpGet("registrar/average-gpa-per-course")]
    public async Task<IActionResult> GetAverageGpaPerCourse()
    {
        var list = await context.Enrollments
            .GroupBy(e => e.Course.Title)
            .Select(g => new { Course = g.Key, AverageGPA = g.Average(e => e.Student.GPA) })
            .ToListAsync();
        // SQL: GROUP BY with AVG aggregation in the database
        return Ok(list);
    }

    // Query 4A: Which students have zero enrollments? (subquery approach)
    [HttpGet("registrar/unenrolled-students-subquery")]
    public async Task<IActionResult> GetUnenrolledStudentsSubquery()
    {
        var list = await context.Students
            .Where(s => !s.Enrollments.Any())
            .Select(s => s.Name)
            .ToListAsync();
        // SQL: NOT EXISTS (SELECT 1 FROM "Enrollments" ...)
        return Ok(list);
    }

    // Query 4B: Which students have zero enrollments? (EF Core 10 LeftJoin approach)
    [HttpGet("registrar/unenrolled-students-leftjoin")]
    public async Task<IActionResult> GetUnenrolledStudentsLeftJoin()
    {
        var list = await context.Students
            .LeftJoin(context.Enrollments, s => s.Id, e => e.StudentId, (s, e) => new { s, e })
            .Where(x => x.e == null)
            .Select(x => x.s.Name)
            .ToListAsync();
        // SQL: LEFT JOIN ... WHERE ... IS NULL
        return Ok(list);
    }
}
