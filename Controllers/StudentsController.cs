using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TmsApi.Data;

[ApiController]
[Route("api/students")]
public class StudentsController(IStudentService studentService, TmsDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var students = await studentService.GetAllAsync();
        return Ok(students);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var student = await studentService.GetByIdAsync(id);
        return student is not null ? Ok(student) : NotFound();
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateStudentRequest request)
    {
        var record = await studentService.AddAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = record.Id }, record);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var deleted = await studentService.DeleteAsync(id);
        return deleted ? NoContent() : NotFound();
    }

    /// <summary>
    /// Exercise 8: Update name/GPA with optimistic concurrency.
    /// The caller must send the Version they last saw; if another request updated
    /// the student first, this returns 409 Conflict.
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateStudentRequest request)
    {
        try
        {
            var updated = await studentService.UpdateAsync(id, request);
            return updated is not null ? Ok(updated) : NotFound();
        }
        catch (DbUpdateConcurrencyException)
        {
            // Another request modified the row between the caller's GET and this PUT.
            // The xmin (row version) changed; EF refuses to overwrite.
            return Conflict(new
            {
                Error = "ConcurrencyConflict",
                Message = "The student record was modified by another user. Please reload and try again."
            });
        }
    }

    // -------------------------------------------------------------------------
    // Exercise 9 — Soft-delete a student
    // -------------------------------------------------------------------------
    /// <summary>
    /// Soft-deletes a student by setting IsDeleted = true.
    /// The student disappears from all normal queries (HasQueryFilter).
    /// Use GET /api/students/deleted to see them again as an admin.
    /// </summary>
    [HttpDelete("{id}/soft")]
    public async Task<IActionResult> SoftDelete(int id, CancellationToken ct)
    {
        var affected = await db.Students
            .Where(s => s.Id == id)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(s => s.IsDeleted, true),
                ct);

        return affected > 0 ? NoContent() : NotFound();
    }

    // -------------------------------------------------------------------------
    // Exercise 9 — Admin restore: view soft-deleted students
    // -------------------------------------------------------------------------
    /// <summary>
    /// Admin endpoint. Returns students flagged as deleted — normally hidden by
    /// HasQueryFilter. IgnoreQueryFilters() overrides the filter for this query only.
    /// </summary>
    [HttpGet("deleted")]
    public async Task<IActionResult> GetDeleted(CancellationToken ct)
    {
        var deleted = await db.Students
            .IgnoreQueryFilters()           // Bypass HasQueryFilter(s => !s.IsDeleted)
            .Where(s => s.IsDeleted)        // Only the soft-deleted ones
            .Select(s => new { s.Id, s.RegistrationNumber, s.Name, s.GPA, s.IsDeleted })
            .ToListAsync(ct);

        return Ok(deleted);
    }

    // -------------------------------------------------------------------------
    // Exercise 9 — Admin restore a soft-deleted student
    // -------------------------------------------------------------------------
    /// <summary>
    /// Admin endpoint. Restores a soft-deleted student by setting IsDeleted = false.
    /// </summary>
    [HttpPost("{id}/restore")]
    public async Task<IActionResult> Restore(int id, CancellationToken ct)
    {
        var affected = await db.Students
            .IgnoreQueryFilters()           // Must bypass filter to find the deleted row
            .Where(s => s.Id == id && s.IsDeleted)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(s => s.IsDeleted, false),
                ct);

        return affected > 0 ? Ok(new { Message = $"Student {id} restored." }) : NotFound();
    }
}
