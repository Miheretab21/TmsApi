using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TmsApi.Application.DTOs;
using TmsApi.Application.Interfaces;

namespace TmsApi.Api.Controllers;

[ApiController]
[Route("api/courses/{courseId:int}/assessments")]
[Tags("Assessments")]
[Produces("application/json")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
public class AssessmentsController(
    ICourseService courseService,
    IAssessmentService assessmentService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<AssessmentResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("List assessments for a course")]
    public async Task<IActionResult> GetAssessments(int courseId, CancellationToken ct)
    {
        var course = await courseService.GetByIdAsync(courseId, ct);
        if (course is null) return NotFound();

        var assessments = await assessmentService.GetByCourseAsync(courseId, ct);
        return Ok(assessments);
    }

    [HttpGet("{id:int}", Name = nameof(GetAssessmentById))]
    [ProducesResponseType(typeof(AssessmentResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Get an assessment by ID")]
    public async Task<IActionResult> GetAssessmentById(int courseId, int id, CancellationToken ct)
    {
        var assessment = await assessmentService.GetByIdAsync(courseId, id, ct);
        return assessment is not null ? Ok(assessment) : NotFound();
    }

    [HttpPost]
    [Authorize(Roles = "Instructor,Admin")]
    [ProducesResponseType(typeof(AssessmentResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Create an assessment for a course")]
    [EndpointDescription("Returns 404 if the course does not exist.")]
    public async Task<IActionResult> CreateAssessment(int courseId, CreateAssessmentRequest request, CancellationToken ct)
    {
        var course = await courseService.GetByIdAsync(courseId, ct);
        if (course is null) return NotFound();

        var result = await assessmentService.CreateAsync(courseId, request, ct);
        return CreatedAtAction(nameof(GetAssessmentById), new { courseId, id = result.Id }, result);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Delete an assessment")]
    public async Task<IActionResult> DeleteAssessment(int courseId, int id, CancellationToken ct)
    {
        var deleted = await assessmentService.DeleteAsync(courseId, id, ct);
        return deleted ? NoContent() : NotFound();
    }
}
