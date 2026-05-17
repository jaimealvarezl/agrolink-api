using AgroLink.Application.Features.Dashboard.DTOs;
using AgroLink.Application.Features.Dashboard.Queries.GetDashboardSummary;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgroLink.Api.Controllers;

[Route("api/farms/{farmId}/dashboard-summary")]
[Authorize(Policy = "FarmViewerAccess")]
public class DashboardController(IMediator mediator) : BaseController
{
    [HttpGet]
    public async Task<ActionResult<DashboardSummaryDto>> GetSummary(
        int farmId,
        CancellationToken cancellationToken
    )
    {
        var result = await mediator.Send(new GetDashboardSummaryQuery(farmId), cancellationToken);
        return Ok(result);
    }
}
