using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using TmsApi.Api.Hubs;
using TmsApi.Application.DTOs;
using TmsApi.Application.Hubs;
using TmsApi.Application.Interfaces;

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
        // Your existing approval logic ...
        var success = await enrollmentService.ApproveAsync(id, ct);
        if (!success) return NotFound();

        // After the database commit succeeds, broadcast to all connected Angular clients
        await hubContext.Clients.All
            .ReceiveEnrollmentStatusUpdated(id, "Approved");

        return NoContent();
    }
}
