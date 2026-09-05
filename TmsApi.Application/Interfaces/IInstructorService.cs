namespace TmsApi.Application.Interfaces;

public record CreateInstructorRequest(string Email, string Password, string FirstName, string LastName, string? Department = null);
public record UpdateInstructorRequest(string FirstName, string LastName, string? Department = null);
public record InstructorResponse(string Id, string Email, string FirstName, string LastName, string? Department);

public interface IInstructorService
{
    Task<InstructorResponse> CreateAsync(CreateInstructorRequest request, CancellationToken ct);
    Task<InstructorResponse?> GetByIdAsync(string id, CancellationToken ct);
    Task<IReadOnlyList<InstructorResponse>> GetAllAsync(CancellationToken ct);
    Task<bool> UpdateAsync(string id, UpdateInstructorRequest request, CancellationToken ct);
    Task<bool> DeleteAsync(string id, CancellationToken ct);
}
