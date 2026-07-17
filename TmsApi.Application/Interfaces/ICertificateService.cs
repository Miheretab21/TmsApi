using TmsApi.Application.DTOs;

namespace TmsApi.Application.Interfaces;

public interface ICertificateService
{
    Task<IReadOnlyList<CertificateResponseDto>> GetByStudentAsync(int studentId, CancellationToken ct);
    Task<CertificateResponseDto?> GetByIdAsync(int studentId, int id, CancellationToken ct);
    Task<CertificateResponseDto> IssueAsync(int studentId, IssueCertificateRequest request, CancellationToken ct);
}
