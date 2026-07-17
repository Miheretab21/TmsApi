namespace TmsApi.Application.DTOs;

public record AssessmentResponseDto(
    int Id,
    int CourseId,
    string Title,
    decimal MaxScore,
    decimal Weight);
