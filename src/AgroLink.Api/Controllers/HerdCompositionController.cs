using AgroLink.Application.Features.HerdComposition.DTOs;
using AgroLink.Application.Features.HerdComposition.Queries.GetHerdComposition;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgroLink.Api.Controllers;

[Route("api/farms/{farmId}/herd-composition")]
[Authorize(Policy = "FarmEditorAccess")]
public class HerdCompositionController(IMediator mediator) : BaseController
{
    [HttpGet]
    public async Task<ActionResult<HerdCompositionDto>> GetHerdComposition(
        int farmId,
        CancellationToken cancellationToken
    )
    {
        var result = await mediator.Send(new GetHerdCompositionQuery(farmId), cancellationToken);
        return Ok(result);
    }
}
