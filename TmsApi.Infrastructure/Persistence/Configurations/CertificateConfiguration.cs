using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TmsApi.Domain.Entities;
namespace TmsApi.Infrastructure.Persistence;
public class CertificateConfiguration : IEntityTypeConfiguration<Certificate>
{
    public void Configure(EntityTypeBuilder<Certificate> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.SerialNumber)
               .IsRequired()
               .HasMaxLength(50);

        builder.Property(c => c.IssuedAt)
               .IsRequired();

        builder.HasIndex(c => c.SerialNumber)
               .IsUnique();

        builder.HasOne(c => c.Student)
               .WithMany(s => s.Certificates)
               .HasForeignKey(c => c.StudentId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.Course)
               .WithMany(c => c.Certificates)
               .HasForeignKey(c => c.CourseId)
               .OnDelete(DeleteBehavior.Restrict);

        // Matching query filter for Student's soft-delete filter
        builder.HasQueryFilter(c => !c.Student.IsDeleted);
    }
}
