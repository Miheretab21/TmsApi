using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TmsApi.Infrastructure.Persistence;

namespace TmsApi.Api.Controllers;

[ApiController]
[Route("api/test")]
public class TestController(TmsDbContext context) : ControllerBase
{
    private static bool IsHonorRoll(decimal gpa) => gpa >= 3.5m;

    [HttpGet("translation-fail")]
    public IActionResult TestTranslationFail()
    {
        try
        {
            var students = context.Students
                .Where(s => IsHonorRoll(s.GPA))
                .ToList();
            return Ok(students);
        }
        catch (Exception ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }

    [HttpGet("translation-fix-server")]
    public IActionResult TestTranslationFixServer()
    {
        var students = context.Students
            .Where(s => s.GPA >= 3.5m)
            .ToList();
        return Ok(students);
    }

    [HttpGet("translation-fix-client")]
    public IActionResult TestTranslationFixClient()
    {
        var students = context.Students
            .AsEnumerable()
            .Where(s => IsHonorRoll(s.GPA))
            .ToList();
        return Ok(students);
    }

    [HttpGet("deferred")]
    public IActionResult TestDeferred()
    {
        var query        = context.Students.Where(s => s.GPA >= 3.0m);
        var orderedQuery = query.OrderBy(s => s.Name);
        var results      = orderedQuery.ToList();
        return Ok(results);
    }
}
