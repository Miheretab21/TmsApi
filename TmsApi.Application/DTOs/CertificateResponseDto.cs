namespace TmsApi.Application.DTOs;

public record CertificateResponseDto(
    int Id,
    string SerialNumber,
    int StudentId,
    int CourseId,
    DateTime IssuedAt);
