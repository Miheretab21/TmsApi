using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TmsApi.Application.DTOs;
using TmsApi.Application.Interfaces;
using TmsApi.Domain.Entities;
using TmsApi.Infrastructure.Persistence;

namespace TmsApi.Infrastructure.Services;

public class CertificateService(TmsDbContext context, ILogger<CertificateService> logger) : ICertificateService
{
    public Task<IReadOnlyList<CertificateResponseDto>> GetByStudentAsync(int studentId, CancellationToken ct) =>
        context.Certificates
            .AsNoTracking()
            .Where(c => c.StudentId == studentId)
            .Select(c => new CertificateResponseDto(c.Id, c.SerialNumber, c.StudentId, c.CourseId, c.IssuedAt))
            .ToListAsync(ct)
            .ContinueWith(t => (IReadOnlyList<CertificateResponseDto>)t.Result, ct);

    public Task<CertificateResponseDto?> GetByIdAsync(int studentId, int id, CancellationToken ct) =>
        context.Certificates
            .AsNoTracking()
            .Where(c => c.Id == id && c.StudentId == studentId)
            .Select(c => new CertificateResponseDto(c.Id, c.SerialNumber, c.StudentId, c.CourseId, c.IssuedAt))
            .FirstOrDefaultAsync(ct);

    public async Task<CertificateResponseDto> IssueAsync(int studentId, IssueCertificateRequest request, CancellationToken ct)
    {
        var enrolled = await context.Enrollments
            .AsNoTracking()
            .AnyAsync(e => e.StudentId == studentId && e.CourseId == request.CourseId, ct);

        if (!enrolled)
            throw new InvalidOperationException($"Student {studentId} is not enrolled in course {request.CourseId}.");

        var alreadyIssued = await context.Certificates
            .AsNoTracking()
            .AnyAsync(c => c.StudentId == studentId && c.CourseId == request.CourseId, ct);

        if (alreadyIssued)
            throw new InvalidOperationException($"A certificate for student {studentId} in course {request.CourseId} already exists.");

        var certificate = new Certificate
        {
            SerialNumber = $"TMS-{studentId:D4}-{request.CourseId:D4}-{DateTime.UtcNow:yyyyMMddHHmmss}",
            StudentId    = studentId,
            CourseId     = request.CourseId,
            IssuedAt     = DateTime.UtcNow
        };
        context.Certificates.Add(certificate);
        await context.SaveChangesAsync(ct);
        logger.LogInformation("Issued certificate {SerialNumber} to student {StudentId}", certificate.SerialNumber, studentId);
        return (await GetByIdAsync(studentId, certificate.Id, ct))!;
    }
}
