// using Microsoft.Extensions.DependencyInjection;

// public class EnrollmentWorker
// {
//     private readonly IEnrollmentService _enrollmentService;

//     // BUG: A Singleton constructor should never take a Scoped service directly
//     public EnrollmentWorker(IEnrollmentService enrollmentService)
//     {
//         _enrollmentService = enrollmentService;
//     }

//     public void ProcessBatch()
//     {
//         // Simulate background work
//         var enrollments = _enrollmentService.GetAllAsync().Result;
//     }
// } this generates an error because it is a buggy registration


public class EnrollmentWorker
{
    private readonly IServiceScopeFactory _scopeFactory;

    public EnrollmentWorker(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public void ProcessBatch()
    {
        using var scope = _scopeFactory.CreateScope();
        var enrollmentService = scope.ServiceProvider.GetRequiredService<IEnrollmentService>();
        var enrollments = enrollmentService.GetAllAsync().GetAwaiter().GetResult();
    }
}

