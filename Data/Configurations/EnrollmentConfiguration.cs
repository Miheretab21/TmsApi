using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TmsApi.Entities;

namespace TmsApi.Data.Configurations;

public class EnrollmentConfiguration : IEntityTypeConfiguration<Enrollment>
{
    public void Configure(EntityTypeBuilder<Enrollment> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.EnrolledAt)
               .IsRequired();

        builder.Property(e => e.Grade)
               .HasPrecision(4, 2);

        builder.Property(e => e.IsArchived)
               .IsRequired()
               .HasDefaultValue(false);

        // Archived filter (Exercise 9).
        // Normal queries exclude archived enrollments automatically.
        // Use IgnoreQueryFilters() in admin endpoints to see them.
        builder.HasQueryFilter(e => !e.IsArchived);

        // Restrict delete — app must unenroll students before deleting a student or course to preserve enrollment history
        builder.HasOne(e => e.Student)
               .WithMany(s => s.Enrollments)
               .HasForeignKey(e => e.StudentId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Course)
               .WithMany(c => c.Enrollments)
               .HasForeignKey(e => e.CourseId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
