using Microsoft.AspNetCore.Authorization;

namespace TmsApi.Api.Authorization;

/// <summary>
/// Marker requirement: the current user must be the course owner or an Admin.
/// </summary>
public class CourseInstructorRequirement : IAuthorizationRequirement { }
