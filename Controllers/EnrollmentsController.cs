using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TmsApi.Data;

[ApiController]
[Route("api/enrollments")]
public class EnrollmentsController(IEnrollmentService enrollmentService, TmsDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var enrollments = await enrollmentService.GetAllAsync();
        return Ok(enrollments);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var record = await enrollmentService.GetByIdAsync(id);
        return record is not null ? Ok(record) : NotFound();
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateEnrollmentRequest request)
    {
        var record = await enrollmentService.EnrollAsync(request.StudentId, request.CourseCode);
        return CreatedAtAction(nameof(GetById), new { id = record.Id }, record);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var deleted = await enrollmentService.DeleteAsync(id);
        return deleted ? NoContent() : NotFound();
    }

    // -------------------------------------------------------------------------
    // Exercise 9 — Bulk archive using ExecuteUpdateAsync (set-based, not row-by-row)
    // -------------------------------------------------------------------------
    /// <summary>
    /// Archives all enrollments older than the specified cutoff date in a single
    /// UPDATE statement. Does not load rows into memory; executes directly on the DB.
    /// </summary>
    [HttpPost("bulk-archive")]
    public async Task<IActionResult> BulkArchive([FromQuery] DateTime cutoffDate, CancellationToken ct)
    {
        // Npgsql requires UTC for timestamp with time zone — convert if caller sent Unspecified
        var cutoffUtc = cutoffDate.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(cutoffDate, DateTimeKind.Utc)
            : cutoffDate.ToUniversalTime();

        // ExecuteUpdateAsync — single SQL UPDATE statement, no tracking, no round-trips per row
        var affected = await db.Enrollments
            .Where(e => e.EnrolledAt < cutoffUtc)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(e => e.IsArchived, true),
                ct);

        return Ok(new
        {
            Message = $"Archived {affected} enrollment(s) older than {cutoffUtc:yyyy-MM-dd}.",
            Affected = affected
        });
    }

    // -------------------------------------------------------------------------
    // Exercise 9 — Admin restore: view archived enrollments
    // -------------------------------------------------------------------------
    /// <summary>
    /// Admin-only endpoint. Shows archived enrollments that are normally hidden
    /// by the HasQueryFilter. Use IgnoreQueryFilters() to see them.
    /// </summary>
    [HttpGet("archived")]
    public async Task<IActionResult> GetArchived(CancellationToken ct)
    {
        var archived = await db.Enrollments
            .IgnoreQueryFilters()              // See everything, including archived
            .Where(e => e.IsArchived)          // Filter to only archived
            .Select(e => new
            {
                e.Id,
                e.StudentId,
                e.CourseId,
                e.EnrolledAt,
                e.Grade,
                e.IsArchived
            })
            .ToListAsync(ct);

        return Ok(archived);
    }
}

public record CreateEnrollmentRequest(string StudentId, string CourseCode);
