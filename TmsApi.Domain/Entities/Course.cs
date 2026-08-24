namespace TmsApi.Domain.Entities;
public class Course
{
    public int Id { get; set; }
    public required string Code { get; set; }
    public required string Title { get; set; }
    public int MaxCapacity { get; set; }

    /// <summary>
    /// The Identity user ID of the instructor who owns this course.
    /// Null for courses not yet assigned to an instructor.
    /// </summary>
    public string? InstructorId { get; set; }

    public ICollection<Enrollment> Enrollments { get; set; } = [];
    public ICollection<Assessment> Assessments { get; set; } = [];
    public ICollection<Certificate> Certificates { get; set; } = [];
}
