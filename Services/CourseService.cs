public interface ICourseService
{
    Task<Course> AddAsync(Course course);
    Task<Course?> GetByIdAsync(string code);
    Task<IReadOnlyList<Course>> GetAllAsync();
    Task<bool> DeleteAsync(string code);
}

public class CourseService : ICourseService
{
    private readonly Dictionary<string, Course> _store = new();
    private readonly ILogger<CourseService> _logger;

    public CourseService(ILogger<CourseService> logger)
    {
        _logger = logger;
    }

    public Task<Course> AddAsync(Course course)
    {
        if (_store.ContainsKey(course.Code))
        {
            _logger.LogWarning("Course {CourseCode} already exists", course.Code);
            return Task.FromResult(_store[course.Code]);
        }

        _store[course.Code] = course;
        _logger.LogInformation("Added course {CourseCode}", course.Code);
        return Task.FromResult(course);
    }

    public Task<Course?> GetByIdAsync(string code)
    {
        _store.TryGetValue(code, out var course);
        if (course is null)
        {
            _logger.LogWarning("Course {CourseCode} not found", code);
        }
        return Task.FromResult(course);
    }

    public Task<IReadOnlyList<Course>> GetAllAsync()
    {
        IReadOnlyList<Course> all = _store.Values.ToList();
        return Task.FromResult(all);
    }

    public Task<bool> DeleteAsync(string code)
    {
        var removed = _store.Remove(code);
        if (removed)
        {
            _logger.LogInformation("Deleted course {CourseCode}", code);
        }
        else
        {
            _logger.LogWarning("Delete failed: Course {CourseCode} not found", code);
        }
        return Task.FromResult(removed);
    }
}