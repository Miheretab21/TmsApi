using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TmsApi.Application.Courses.Commands;
using TmsApi.Application.Courses.Queries;
using TmsApi.Application.Interfaces;

namespace TmsApi.Api.Controllers.V2;

[ApiController]
[Route("api/v{version:apiVersion}/courses")]
[ApiVersion("2.0")]
public class CoursesController(ICachedCourseService cachedCourseService, IMediator mediator) : ControllerBase
{
    [HttpGet("search")]
    [EnableRateLimiting("search")]
    public async Task<IActionResult> SearchCourses(
        [FromQuery] string? term, CancellationToken ct)
    {
        var results = await mediator.Send(new SearchCoursesQuery(term), ct);
        return Ok(results);
    }
    /// <summary>
    /// Returns all courses. Responses are served from HybridCache after the first
    /// database hit, with stampede protection ensuring only one DB query fires
    /// on a cold-cache burst.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetCourses(CancellationToken ct)
    {
        var courses = await cachedCourseService.GetAllCoursesAsync(ct);
        return Ok(new { data = courses, meta = new { totalCount = courses.Count } });
    }

    /// <summary>Returns a single course by its code, served from HybridCache.</summary>
    [HttpGet("{code}")]
    public async Task<IActionResult> GetCourse(string code, CancellationToken ct)
    {
        var course = await cachedCourseService.GetCourseAsync(code, ct);
        return Ok(course);
    }

    /// <summary>Update a course and invalidate cache.</summary>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateCourse(
        int id,
        [FromBody] UpdateCourseRequest request,
        CancellationToken ct)
    {
        var success = await mediator.Send(
            new UpdateCourseCommand(id, request.Title, request.MaxCapacity), ct);

        return success ? NoContent() : NotFound();
    }
}

public record UpdateCourseRequest(string Title, int MaxCapacity);

