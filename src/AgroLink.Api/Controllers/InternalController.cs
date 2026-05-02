using AgroLink.Application.Features.VoiceCommands.Commands.DeleteStaleVoiceCommandJobs;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace AgroLink.Api.Controllers;

/// <summary>
///     Internal endpoints invoked by Cloud Scheduler. Protected by a shared secret header.
/// </summary>
[ApiController]
[Route("api/internal")]
public class InternalController(IMediator mediator, IConfiguration configuration) : ControllerBase
{
    [HttpPost("cleanup")]
    public async Task<IActionResult> RunCleanup(
        [FromHeader(Name = "X-Scheduler-Secret")] string? secret,
        CancellationToken ct
    )
    {
        var expected = configuration["Internal:SchedulerSecret"];
        if (string.IsNullOrEmpty(expected) || secret != expected)
        {
            return Unauthorized();
        }

        var deleted = await mediator.Send(new DeleteStaleVoiceCommandJobsCommand(), ct);
        return Ok(new { deleted });
    }
}
