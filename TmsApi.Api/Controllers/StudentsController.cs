using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TmsApi.Application.DTOs;
using TmsApi.Application.Interfaces;
using TmsApi.Domain.Entities;
using TmsApi.Infrastructure.Persistence;
using TmsApi.Infrastructure.Identity;

namespace TmsApi.Api.Controllers;

[ApiController]
[Route("api/students")]
public class StudentsController(IStudentService studentService, TmsDbContext db, UserManager<TmsUser> userManager) : ControllerBase
{
    private readonly UserManager<TmsUser> _userManager = userManager;
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var students = await studentService.GetAllAsync();
        return Ok(students);
    }

    /// <summary>
    /// Get enrollments for the currently authenticated user (student).
    /// Looks up the student by matching the user's full name, then returns their enrollments.
    /// </summary>
    [HttpGet("me/enrollments")]
    [Authorize]
    [ProducesResponseType(typeof(IReadOnlyList<EnrollmentResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyEnrollments(CancellationToken ct)
    {
        // Get current user ID from claims
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null) return Unauthorized();

        // Look up student by matching name
        var fullName = $"{user.FirstName} {user.LastName}".Trim();
        var student = await db.Students
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Name == fullName, ct);

        // Fallback 1: try email prefix search
        if (student == null && !string.IsNullOrEmpty(user.Email))
        {
            var emailPrefix = user.Email.Split('@')[0];
            student = await db.Students
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Name.ToLower().Contains(emailPrefix.ToLower()), ct);
        }

        if (student is null)
        {
            return NotFound(new { detail = $"No student record found for user '{fullName}'. Contact administrator to link your user account to a student record." });
        }

        // Get all enrollments for this student
        var enrollments = await db.Enrollments
            .AsNoTracking()
            .Include(e => e.Course)
            .Include(e => e.Student)
            .Where(e => e.StudentId == student.Id)
            .Select(e => new EnrollmentResponseDto(
                e.Id.ToString(),
                e.StudentId,
                e.Student.Name,
                e.CourseId,
                e.Course.Code,
                e.Course.Title,
                e.Status.ToString(),
                e.EnrolledAt,
                e.Grade))
            .ToListAsync(ct);

        return Ok(enrollments);
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

    [HttpPost("{studentId:int}/link-user/{userId}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [EndpointSummary("Link an Identity user to a student record (Admin only)")]
    public async Task<IActionResult> LinkUserToStudent(int studentId, string userId, CancellationToken ct)
    {
        var student = await db.Students.FirstOrDefaultAsync(s => s.Id == studentId, ct);
        if (student is null) return NotFound(new { detail = "Student not found." });

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null) return NotFound(new { detail = "User not found." });

        // Update student name to match user's full name (creates the link)
        var fullName = $"{user.FirstName} {user.LastName}".Trim();
        student.Name = fullName;
        await db.SaveChangesAsync(ct);

        return NoContent();
    }

    [HttpGet("{id:int}/enrollments")]
    [ProducesResponseType(typeof(IReadOnlyList<EnrollmentResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Get all enrollments for a student")]
    public async Task<IActionResult> GetEnrollments(int id, CancellationToken ct)
    {
        var student = await db.Students.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id, ct);
        if (student is null) return NotFound();

        var enrollments = await db.Enrollments
            .AsNoTracking()
            .Include(e => e.Course)
            .Include(e => e.Student)
            .Where(e => e.StudentId == id)
            .Select(e => new EnrollmentResponseDto(
                e.Id.ToString(),
                e.StudentId,
                e.Student.Name,
                e.CourseId,
                e.Course.Code,
                e.Course.Title,
                e.Status.ToString(),
                e.EnrolledAt,
                e.Grade))
            .ToListAsync(ct);

        return Ok(enrollments);
    }
}
