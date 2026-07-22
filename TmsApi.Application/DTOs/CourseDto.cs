namespace TmsApi.Application.DTOs;

/// <summary>
/// Flat DTO used by the cache layer (HybridCache).
/// Separate from CourseResponseDto so schema versioning in CacheKeys
/// can be bumped independently of the API contract DTO.
/// </summary>
public record CourseDto(
    int    Id,
    string Title,
    string Code,
    int    MaxCapacity,
    int    EnrollmentCount);
