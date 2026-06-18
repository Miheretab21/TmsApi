using TmsApi.Entities;

public interface IEnrollmentService
{
    Task<Enrollment> EnrollAsync(string studentId, string courseCode);
    Task<Enrollment?> GetByIdAsync(string id);
    Task<IReadOnlyList<Enrollment>> GetAllAsync();
    Task<bool> DeleteAsync(string id);
}

public class EnrollmentService : IEnrollmentService
{
    private readonly Dictionary<int, Enrollment> _store = new();
    private int _nextId = 1;
    private readonly ILogger<EnrollmentService> _logger;

    public EnrollmentService(ILogger<EnrollmentService> logger)
    {
        _logger = logger;
    }

    public Task<Enrollment> EnrollAsync(string studentId, string courseCode)
    {
        // Note: with in-memory store, StudentId/CourseId are ints; we parse or use 0 as placeholder
        var existing = _store.Values
            .FirstOrDefault(e => e.StudentId.ToString() == studentId && e.CourseId.ToString() == courseCode);

        if (existing is not null)
        {
            _logger.LogWarning(
                "Duplicate enrollment attempt {StudentId} already in {CourseCode} (record {EnrollmentId})",
                studentId, courseCode, existing.Id);
            return Task.FromResult(existing);
        }

        var record = new Enrollment
        {
            Id = _nextId++,
            StudentId = int.TryParse(studentId, out var sid) ? sid : 0,
            CourseId = int.TryParse(courseCode, out var cid) ? cid : 0,
            EnrolledAt = DateTime.UtcNow
        };
        _store[record.Id] = record;

        _logger.LogInformation(
            "Enrolled {StudentId} in {CourseCode} record {EnrollmentId}",
            studentId, courseCode, record.Id);

        return Task.FromResult(record);
    }

    public Task<Enrollment?> GetByIdAsync(string id)
    {
        Enrollment? record = null;
        if (int.TryParse(id, out var intId))
        {
            _store.TryGetValue(intId, out record);
        }
        if (record is null)
        {
            _logger.LogWarning("Enrollment {EnrollmentId} not found", id);
        }
        return Task.FromResult(record);
    }

    public Task<IReadOnlyList<Enrollment>> GetAllAsync()
    {
        IReadOnlyList<Enrollment> all = _store.Values.ToList();
        return Task.FromResult(all);
    }

    public Task<bool> DeleteAsync(string id)
    {
        bool removed = false;
        if (int.TryParse(id, out var intId))
        {
            removed = _store.Remove(intId);
        }
        if (removed)
        {
            _logger.LogInformation("Deleted enrollment {EnrollmentId}", id);
        }
        else
        {
            _logger.LogWarning("Delete failed enrollment {EnrollmentId} not found", id);
        }
        return Task.FromResult(removed);
    }
}

public class TmsDatabaseException(string message) : Exception(message);
