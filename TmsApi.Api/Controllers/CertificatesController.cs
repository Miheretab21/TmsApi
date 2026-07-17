using Microsoft.AspNetCore.Mvc;
using TmsApi.Application.DTOs;
using TmsApi.Application.Interfaces;

namespace TmsApi.Api.Controllers;

[ApiController]
[Route("api/students/{studentId:int}/certificates")]
[Tags("Certificates")]
[Produces("application/json")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
public class CertificatesController(
    IStudentService studentService,
    ICertificateService certificateService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<CertificateResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("List certificates for a student")]
    public async Task<IActionResult> GetCertificates(int studentId, CancellationToken ct)
    {
        var student = await studentService.GetByIdAsync(studentId.ToString());
        if (student is null) return NotFound();

        var certificates = await certificateService.GetByStudentAsync(studentId, ct);
        return Ok(certificates);
    }

    [HttpGet("{id:int}", Name = nameof(GetCertificateById))]
    [ProducesResponseType(typeof(CertificateResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Get a certificate by ID")]
    public async Task<IActionResult> GetCertificateById(int studentId, int id, CancellationToken ct)
    {
        var certificate = await certificateService.GetByIdAsync(studentId, id, ct);
        return certificate is not null ? Ok(certificate) : NotFound();
    }

    [HttpPost]
    [ProducesResponseType(typeof(CertificateResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [EndpointSummary("Issue a certificate to a student")]
    [EndpointDescription("Returns 404 if the student does not exist. Returns 409 if not enrolled or certificate already exists.")]
    public async Task<IActionResult> IssueCertificate(int studentId, IssueCertificateRequest request, CancellationToken ct)
    {
        var student = await studentService.GetByIdAsync(studentId.ToString());
        if (student is null) return NotFound();

        try
        {
            var result = await certificateService.IssueAsync(studentId, request, ct);
            return CreatedAtAction(nameof(GetCertificateById), new { studentId, id = result.Id }, result);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new ProblemDetails
            {
                Title  = "Certificate cannot be issued",
                Detail = ex.Message,
                Status = StatusCodes.Status409Conflict
            });
        }
    }
}
