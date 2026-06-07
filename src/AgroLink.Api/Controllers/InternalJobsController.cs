using AgroLink.Api.Filters;
using AgroLink.Application.Features.Notifications.Commands.RunBirthWatchAlertScan;
using AgroLink.Application.Features.Notifications.Commands.RunSecadoAlertScan;
using AgroLink.Application.Features.Notifications.DTOs;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace AgroLink.Api.Controllers;

[ApiController]
[Route("api/internal/jobs")]
[ServiceFilter(typeof(InternalJobKeyFilter))]
public class InternalJobsController(IMediator mediator) : ControllerBase
{
    // Cloud Scheduler should also use OIDC token auth with the Cloud Run service account as audience.
    [HttpPost("secado-alert-scan")]
    public async Task<ActionResult<SecadoScanSummaryDto>> RunSecadoAlertScan(
        CancellationToken cancellationToken
    )
    {
        var result = await mediator.Send(new RunSecadoAlertScanCommand(), cancellationToken);
        return Ok(result);
    }

    [HttpPost("birth-watch-alert-scan")]
    public async Task<IActionResult> BirthWatchAlertScan(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new RunBirthWatchAlertScanCommand(), cancellationToken);
        return Ok(result);
    }
}
