using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TmsApi.Application.Interfaces;
using TmsApi.Domain.Entities;
using TmsApi.Infrastructure.Persistence;

namespace TmsApi.Infrastructure.Services;

public class StudentService(TmsDbContext db, ILogger<StudentService> logger) : IStudentService
{
    public async Task<StudentResponse> AddAsync(CreateStudentRequest request)
    {
        var existing = await db.Students.FirstOrDefaultAsync(s => s.RegistrationNumber == request.RegistrationNumber);
        if (existing is not null)
        {
            logger.LogWarning("Student {RegistrationNumber} already exists", request.RegistrationNumber);
            return ToResponse(existing);
        }

        var student = new Student
        {
            RegistrationNumber = request.RegistrationNumber,
            Name     = request.Name,
            GPA      = request.GPA,
            IsActive = request.IsActive
        };
        db.Students.Add(student);
        db.Entry(student).Property("LastUpdated").CurrentValue = DateTime.UtcNow;
        await db.SaveChangesAsync();
        logger.LogInformation("Added student {RegistrationNumber}", student.RegistrationNumber);
        return ToResponse(student);
    }

    public async Task<StudentResponse?> GetByIdAsync(string id)
    {
        Student? student = int.TryParse(id, out var intId)
            ? await db.Students.FindAsync(intId)
            : await db.Students.FirstOrDefaultAsync(s => s.RegistrationNumber == id);

        if (student is null)
        {
            logger.LogWarning("Student {StudentId} not found", id);
            return null;
        }
        return ToResponse(student);
    }

    public async Task<IReadOnlyList<StudentResponse>> GetAllAsync()
    {
        return await db.Students
            .Select(s => new StudentResponse(s.Id, s.RegistrationNumber, s.Name, s.GPA, s.IsActive, s.Version))
            .ToListAsync();
    }

    public async Task<bool> DeleteAsync(string id)
    {
        Student? student = int.TryParse(id, out var intId)
            ? await db.Students.FindAsync(intId)
            : await db.Students.FirstOrDefaultAsync(s => s.RegistrationNumber == id);

        if (student is null)
        {
            logger.LogWarning("Delete failed: Student {StudentId} not found", id);
            return false;
        }
        db.Students.Remove(student);
        await db.SaveChangesAsync();
        logger.LogInformation("Deleted student {StudentId}", id);
        return true;
    }

    public async Task<StudentResponse?> UpdateAsync(int id, UpdateStudentRequest request)
    {
        var student = await db.Students.FindAsync(id);
        if (student is null)
        {
            logger.LogWarning("Update failed: Student {StudentId} not found", id);
            return null;
        }

        db.Entry(student).Property(s => s.Version).OriginalValue = request.Version;
        student.Name = request.Name;
        student.GPA  = request.GPA;
        db.Entry(student).Property("LastUpdated").CurrentValue = DateTime.UtcNow;

        await db.SaveChangesAsync();
        logger.LogInformation("Updated student {StudentId}", id);
        return ToResponse(student);
    }

    private static StudentResponse ToResponse(Student s) =>
        new(s.Id, s.RegistrationNumber, s.Name, s.GPA, s.IsActive, s.Version);
}
