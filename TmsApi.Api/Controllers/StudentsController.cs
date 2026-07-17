using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TmsApi.Application.Interfaces;
using TmsApi.Infrastructure.Persistence;

namespace TmsApi.Api.Controllers;

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
            return Conflict(new
            {
                Error   = "ConcurrencyConflict",
                Message = "The student record was modified by another user. Please reload and try again."
            });
        }
    }

    [HttpDelete("{id}/soft")]
    public async Task<IActionResult> SoftDelete(int id, CancellationToken ct)
    {
        var affected = await db.Students
            .Where(s => s.Id == id)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(s => s.IsDeleted, true), ct);

        return affected > 0 ? NoContent() : NotFound();
    }

    [HttpGet("deleted")]
    public async Task<IActionResult> GetDeleted(CancellationToken ct)
    {
        var deleted = await db.Students
            .IgnoreQueryFilters()
            .Where(s => s.IsDeleted)
            .Select(s => new { s.Id, s.RegistrationNumber, s.Name, s.GPA, s.IsDeleted })
            .ToListAsync(ct);

        return Ok(deleted);
    }

    [HttpPost("{id}/restore")]
    public async Task<IActionResult> Restore(int id, CancellationToken ct)
    {
        var affected = await db.Students
            .IgnoreQueryFilters()
            .Where(s => s.Id == id && s.IsDeleted)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(s => s.IsDeleted, false), ct);

        return affected > 0 ? Ok(new { Message = $"Student {id} restored." }) : NotFound();
    }
}
