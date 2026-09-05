namespace TmsApi.Application.DTOs;

public record EnrollmentResponseDto(
    string Id,
    int StudentId,
    string StudentName,
    int CourseId,
    string CourseCode,
    string CourseName,
    string Status,
    DateTime EnrolledAt,
    decimal? Grade = null);


