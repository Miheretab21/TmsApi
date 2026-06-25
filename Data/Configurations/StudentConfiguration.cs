using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TmsApi.Entities;

namespace TmsApi.Data.Configurations;

public class StudentConfiguration : IEntityTypeConfiguration<Student>
{
    public void Configure(EntityTypeBuilder<Student> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.RegistrationNumber)
               .IsRequired()
               .HasMaxLength(20);

        builder.Property(s => s.Name)
               .IsRequired()
               .HasMaxLength(100);

        builder.Property(s => s.GPA)
               .HasPrecision(3, 2);

        builder.Property(s => s.IsActive)
               .IsRequired();

        // Concurrency token — Npgsql maps this to PostgreSQL's xmin system column.
        // No extra column is created; EF uses xmin in the WHERE clause on UPDATE/DELETE.
        builder.Property(s => s.Version)
               .IsRowVersion();

        // Shadow property for audit stamp (Exercise 8).
        // Lives only in the DB column; never appears in the Student C# class or DTOs.
        builder.Property<DateTime>("LastUpdated")
               .HasDefaultValueSql("CURRENT_TIMESTAMP");

        // Soft-delete filter (Exercise 9).
        // Every normal query automatically appends WHERE "IsDeleted" = false.
        // Use IgnoreQueryFilters() in admin endpoints to see deleted students.
        builder.HasQueryFilter(s => !s.IsDeleted);
    }
}
