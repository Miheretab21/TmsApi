using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TmsApi.Application.Interfaces;

namespace TmsApi.Api.Controllers;

[ApiController]
[Route("api/instructors")]
[Tags("Instructors")]
[Produces("application/json")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
public class InstructorsController(IInstructorService instructorService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<InstructorResponse>), StatusCodes.Status200OK)]
    [EndpointSummary("List all instructors")]
    public async Task<IActionResult> GetInstructors(CancellationToken ct)
    {
        var instructors = await instructorService.GetAllAsync(ct);
        return Ok(instructors);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(InstructorResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Get an instructor by ID")]
    public async Task<IActionResult> GetInstructor(string id, CancellationToken ct)
    {
        var instructor = await instructorService.GetByIdAsync(id, ct);
        return instructor is not null ? Ok(instructor) : NotFound();
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(InstructorResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [EndpointSummary("Create a new instructor")]
    [EndpointDescription("Admin only. Creates an instructor account and assigns the Instructor role.")]
    public async Task<IActionResult> CreateInstructor(CreateInstructorRequest request, CancellationToken ct)
    {
        try
        {
            var result = await instructorService.CreateAsync(request, ct);
            return CreatedAtAction(nameof(GetInstructor), new { id = result.Id }, result);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new ProblemDetails
            {
                Title = "Instructor creation failed",
                Detail = ex.Message,
                Status = StatusCodes.Status409Conflict
            });
        }
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Update an instructor")]
    [EndpointDescription("Admin only.")]
    public async Task<IActionResult> UpdateInstructor(string id, UpdateInstructorRequest request, CancellationToken ct)
    {
        var updated = await instructorService.UpdateAsync(id, request, ct);
        return updated ? NoContent() : NotFound();
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Delete an instructor")]
    [EndpointDescription("Admin only.")]
    public async Task<IActionResult> DeleteInstructor(string id, CancellationToken ct)
    {
        var deleted = await instructorService.DeleteAsync(id, ct);
        return deleted ? NoContent() : NotFound();
    }
}
