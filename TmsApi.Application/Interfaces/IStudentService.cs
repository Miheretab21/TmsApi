namespace TmsApi.Application.Interfaces;

public record CreateStudentRequest(string RegistrationNumber, string Name, decimal GPA, bool IsActive = true);
public record UpdateStudentRequest(string Name, decimal GPA, uint Version);
public record StudentResponse(int Id, string RegistrationNumber, string Name, decimal GPA, bool IsActive, uint Version);

public interface IStudentService
{
    Task<StudentResponse> AddAsync(CreateStudentRequest request);
    Task<StudentResponse?> GetByIdAsync(string id);
    Task<IReadOnlyList<StudentResponse>> GetAllAsync();
    Task<bool> DeleteAsync(string id);
    Task<StudentResponse?> UpdateAsync(int id, UpdateStudentRequest request);
}
