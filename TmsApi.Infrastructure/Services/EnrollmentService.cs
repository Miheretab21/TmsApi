using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TmsApi.Application.DTOs;
using TmsApi.Application.Interfaces;
using TmsApi.Domain.Entities;
using TmsApi.Infrastructure.Persistence;

namespace TmsApi.Infrastructure.Services;

public class EnrollmentService(TmsDbContext context, ILogger<EnrollmentService> logger) : IEnrollmentService
{
    public Task<EnrollmentResponseDto?> GetByIdAsync(int courseId, int id, CancellationToken ct) =>
        context.Enrollments
            .AsNoTracking()
            .Include(e => e.Student)
            .Include(e => e.Course)
            .Where(e => e.Id == id && e.CourseId == courseId)
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
            .FirstOrDefaultAsync(ct);

    public async Task<EnrollmentResponseDto> CreateAsync(int courseId, EnrollStudentRequest request, CancellationToken ct)
    {
        var enrollment = new Enrollment
        {
            CourseId  = courseId,
            StudentId = request.StudentId,
            EnrolledAt = DateTime.UtcNow,
            Status = EnrollmentStatus.Pending
        };
        context.Enrollments.Add(enrollment);
        await context.SaveChangesAsync(ct);
        logger.LogInformation("Enrolled student {StudentId} in course {CourseId}", request.StudentId, courseId);
        return (await GetByIdAsync(courseId, enrollment.Id, ct))!;
    }

    public Task<IReadOnlyList<EnrollmentResponseDto>> GetByCourseAsync(int courseId, CancellationToken ct) =>
        context.Enrollments
            .AsNoTracking()
            .Include(e => e.Student)
            .Include(e => e.Course)
            .Where(e => e.CourseId == courseId)
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
            .ToListAsync(ct)
            .ContinueWith(t => (IReadOnlyList<EnrollmentResponseDto>)t.Result, ct);

    public Task<bool> ExistsAsync(int studentId, string courseCode, CancellationToken ct = default) =>
        context.Enrollments
            .AsNoTracking()
            .Include(e => e.Course)
            .AnyAsync(e => e.StudentId == studentId && e.Course.Code == courseCode, ct);

    public async Task AddAsync(Enrollment enrollment, CancellationToken ct = default)
    {
        context.Enrollments.Add(enrollment);
        await context.SaveChangesAsync(ct);
    }

    public Task<List<Enrollment>> GetByStudentIdAsync(int studentId, CancellationToken ct = default) =>
        context.Enrollments
            .AsNoTracking()
            .Include(e => e.Course)
            .Where(e => e.StudentId == studentId)
            .ToListAsync(ct);

    public Task<IReadOnlyList<EnrollmentListDto>> GetAllAsync(CancellationToken ct) =>
        context.Enrollments
            .AsNoTracking()
            .Include(e => e.Student)
            .Include(e => e.Course)
            .Select(e => new EnrollmentListDto(
                e.Id.ToString(),
                e.StudentId,
                e.Student.Name,
                e.CourseId,
                e.Course.Code,
                e.Course.Title,
                e.Status.ToString(),
                e.EnrolledAt.ToString("o"),
                e.Grade))
            .ToListAsync(ct)
            .ContinueWith(t => (IReadOnlyList<EnrollmentListDto>)t.Result, ct);

    public async Task<bool> ApproveAsync(string id, CancellationToken ct)
    {
        if (!int.TryParse(id, out var intId)) return false;
        var enrollment = await context.Enrollments.FindAsync([intId], ct);
        if (enrollment is null) return false;
        enrollment.Status = EnrollmentStatus.Approved;
        await context.SaveChangesAsync(ct);
        logger.LogInformation("Enrollment {Id} approved", intId);
        return true;
    }

    public Task<Enrollment?> GetEntityByIdAsync(int id, CancellationToken ct) =>
        context.Enrollments
            .Include(e => e.Student)
            .Include(e => e.Course)
            .FirstOrDefaultAsync(e => e.Id == id, ct);

    public async Task UpdateAsync(Enrollment enrollment, CancellationToken ct)
    {
        context.Enrollments.Update(enrollment);
        await context.SaveChangesAsync(ct);
    }
}
