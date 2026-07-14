using System.ComponentModel.DataAnnotations;

namespace TmsApi.Dtos;

public record IssueCertificateRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "CourseId must be a positive integer.")]
    public required int CourseId { get; init; }
}
