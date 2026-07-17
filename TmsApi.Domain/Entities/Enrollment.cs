using System;
namespace TmsApi.Domain.Entities;
public class Enrollment
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public int CourseId { get; set; }
    public decimal? Grade { get; set; }
    public DateTime EnrolledAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Set to true when the enrollment is bulk-archived (Exercise 9).
    /// Kept in the table for audit/recovery; hidden from normal queries via HasQueryFilter.
    /// </summary>
    public bool IsArchived { get; set; } = false;

    public Student Student { get; set; } = null!;
    public Course Course { get; set; } = null!;
}