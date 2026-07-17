using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TmsApi.Application.DTOs;
using TmsApi.Application.Interfaces;
using TmsApi.Domain.Entities;
using TmsApi.Infrastructure.Persistence;

namespace TmsApi.Infrastructure.Services;

public class AssessmentService(TmsDbContext context, ILogger<AssessmentService> logger) : IAssessmentService
{
    public Task<IReadOnlyList<AssessmentResponseDto>> GetByCourseAsync(int courseId, CancellationToken ct) =>
        context.Assessments
            .AsNoTracking()
            .Where(a => a.CourseId == courseId)
            .Select(a => new AssessmentResponseDto(a.Id, a.CourseId, a.Title, a.MaxScore, a.Weight))
            .ToListAsync(ct)
            .ContinueWith(t => (IReadOnlyList<AssessmentResponseDto>)t.Result, ct);

    public Task<AssessmentResponseDto?> GetByIdAsync(int courseId, int id, CancellationToken ct) =>
        context.Assessments
            .AsNoTracking()
            .Where(a => a.Id == id && a.CourseId == courseId)
            .Select(a => new AssessmentResponseDto(a.Id, a.CourseId, a.Title, a.MaxScore, a.Weight))
            .FirstOrDefaultAsync(ct);

    public async Task<AssessmentResponseDto> CreateAsync(int courseId, CreateAssessmentRequest request, CancellationToken ct)
    {
        var assessment = new Assessment
        {
            CourseId = courseId,
            Title    = request.Title,
            MaxScore = request.MaxScore,
            Weight   = request.Weight
        };
        context.Assessments.Add(assessment);
        await context.SaveChangesAsync(ct);
        logger.LogInformation("Created assessment {AssessmentId} for course {CourseId}", assessment.Id, courseId);
        return (await GetByIdAsync(courseId, assessment.Id, ct))!;
    }

    public async Task<bool> DeleteAsync(int courseId, int id, CancellationToken ct)
    {
        var assessment = await context.Assessments
            .FirstOrDefaultAsync(a => a.Id == id && a.CourseId == courseId, ct);
        if (assessment is null) return false;
        context.Assessments.Remove(assessment);
        await context.SaveChangesAsync(ct);
        logger.LogInformation("Deleted assessment {AssessmentId} from course {CourseId}", id, courseId);
        return true;
    }
}
