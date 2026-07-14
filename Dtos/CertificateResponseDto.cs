namespace TmsApi.Dtos;

public record CertificateResponseDto(
    int Id,
    string SerialNumber,
    int StudentId,
    int CourseId,
    DateTime IssuedAt);
