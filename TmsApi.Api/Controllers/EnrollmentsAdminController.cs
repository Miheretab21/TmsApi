using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using TmsApi.Api.Hubs;
using TmsApi.Application.DTOs;
using TmsApi.Application.Hubs;
using TmsApi.Application.Interfaces;
using TmsApi.Domain.Entities;

namespace TmsApi.Api.Controllers;

/// <summary>
/// Flat enrollment endpoints consumed by the Angular EnrollmentStore.
/// GET  /api/enrollments          — all enrollments with student/course names and status
/// POST /api/enrollments/{id}/approve — instructor approval (optimistic update target)
/// </summary>
[ApiController]
[Route("api/enrollments")]
[Tags("Enrollments")]
[Produces("application/json")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
public class EnrollmentsAdminController(
    IEnrollmentService enrollmentService,
    IHubContext<TmsHub, ITmsHubClient> hubContext) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<EnrollmentListDto>), StatusCodes.Status200OK)]
    [EndpointSummary("List all enrollments (instructor view)")]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var enrollments = await enrollmentService.GetAllAsync(ct);
        return Ok(enrollments);
    }

    [HttpPost("{id}/approve")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Approve an enrollment")]
    public async Task<IActionResult> Approve(string id, CancellationToken ct)
    {
        var success = await enrollmentService.ApproveAsync(id, ct);
        if (!success) return NotFound();

        await hubContext.Clients.All
            .ReceiveEnrollmentStatusUpdated(id, "Approved");

        return NoContent();
    }

    [HttpPost("{id}/reject")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Reject an enrollment")]
    public async Task<IActionResult> Reject(string id, CancellationToken ct)
    {
        if (!int.TryParse(id, out var intId)) return NotFound();
        var enrollment = await enrollmentService.GetEntityByIdAsync(intId, ct);
        if (enrollment is null) return NotFound();

        enrollment.Status = EnrollmentStatus.Rejected;
        await enrollmentService.UpdateAsync(enrollment, ct);

        await hubContext.Clients.All
            .ReceiveEnrollmentStatusUpdated(id, "Rejected");

        return NoContent();
    }

    [HttpPost("{id}/grade")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [EndpointSummary("Submit a grade for an enrollment")]
    public async Task<IActionResult> SubmitGrade(string id, [FromBody] SubmitGradeRequest request, CancellationToken ct)
    {
        if (!int.TryParse(id, out var intId)) return NotFound();
        var enrollment = await enrollmentService.GetEntityByIdAsync(intId, ct);
        if (enrollment is null) return NotFound();

        if (request.Score < 0)
            return BadRequest(new ValidationProblemDetails
            {
                Detail = "Score cannot be negative."
            });

        enrollment.Grade = request.Score;
        await enrollmentService.UpdateAsync(enrollment, ct);

        return NoContent();
    }
}

public record SubmitGradeRequest(decimal Score);
