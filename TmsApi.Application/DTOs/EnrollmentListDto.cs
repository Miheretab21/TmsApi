namespace TmsApi.Application.DTOs;

/// <summary>
/// Flat enrollment record returned by GET /api/enrollments.
/// Includes student name, course name, and approval status
/// so the Angular EnrollmentStore can display and filter without
/// secondary requests.
/// </summary>
public record EnrollmentListDto(
    string Id,
    int StudentId,
    string StudentName,
    int CourseId,
    string CourseName,
    string Status,
    string EnrolledAt);
