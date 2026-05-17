using AgroLink.Application.Features.ActivityFeed.DTOs;
using AgroLink.Application.Features.ActivityFeed.Queries.GetFarmActivityFeed;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgroLink.Api.Controllers;

[Route("api/farms/{farmId}/activity-feed")]
[Authorize(Policy = "FarmViewerAccess")]
public class ActivityFeedController(IMediator mediator) : BaseController
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ActivityFeedItemDto>>> GetActivityFeed(
        int farmId,
        [FromQuery] int limit = 5,
        CancellationToken cancellationToken = default
    )
    {
        var result = await mediator.Send(
            new GetFarmActivityFeedQuery(farmId, limit),
            cancellationToken
        );
        return Ok(result);
    }
}
