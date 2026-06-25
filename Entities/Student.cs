namespace TmsApi.Entities;

public class Student
{
    public int Id { get; set; }
    public required string RegistrationNumber { get; set; }
    public required string Name { get; set; }
    public decimal GPA { get; set; }
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Soft-delete flag (Exercise 9). When true, HasQueryFilter excludes this student
    /// from all normal queries. Use IgnoreQueryFilters() in admin endpoints to restore.
    /// </summary>
    public bool IsDeleted { get; set; } = false;

    /// <summary>
    /// Concurrency token — Npgsql maps this to PostgreSQL's xmin system column.
    /// Prevents two staff members from silently overwriting each other's edits.
    /// </summary>
    public uint Version { get; set; }

    public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
    public ICollection<Certificate> Certificates { get; set; } = new List<Certificate>();
}