using AgroLink.Api.DTOs.Paddocks;
using AgroLink.Application.Features.Paddocks.Commands.Create;
using AgroLink.Application.Features.Paddocks.Commands.Delete;
using AgroLink.Application.Features.Paddocks.Commands.Update;
using AgroLink.Application.Features.Paddocks.DTOs;
using AgroLink.Application.Features.Paddocks.Queries.GetByFarm;
using AgroLink.Application.Features.Paddocks.Queries.GetById;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgroLink.Api.Controllers;

[Route("api/farms/{farmId}/paddocks")]
[Authorize(Policy = "FarmViewerAccess")]
public class PaddocksController(IMediator mediator) : BaseController
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<PaddockDto>>> GetAll(int farmId)
    {
        // Replaces GetAll (all user paddocks) and GetByFarm with a scoped GetAll
        var paddocks = await mediator.Send(new GetPaddocksByFarmQuery(farmId));
        return Ok(paddocks);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<PaddockDto>> GetById(int farmId, int id)
    {
        var paddock = await mediator.Send(new GetPaddockByIdQuery(id));
        if (paddock == null)
        {
            return NotFound();
        }

        // Safety check: ensure paddock belongs to farm
        if (paddock.FarmId != farmId)
        {
            return NotFound();
        }

        return Ok(paddock);
    }

    [HttpPost]
    [Authorize(Policy = "FarmAdminAccess")]
    public async Task<ActionResult<PaddockDto>> Create(
        int farmId,
        [FromBody] CreatePaddockRequest request
    )
    {
        try
        {
            var userId = GetCurrentUserId();

            var paddock = await mediator.Send(
                new CreatePaddockCommand(
                    request.Name,
                    farmId, // Use route farmId
                    userId,
                    request.Area,
                    request.AreaType
                )
            );

            return CreatedAtAction(nameof(GetById), new { farmId, id = paddock.Id }, paddock);
        }
        catch (Exception ex)
        {
            return HandleServiceException(ex);
        }
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "FarmAdminAccess")]
    public async Task<ActionResult<PaddockDto>> Update(
        int farmId,
        int id,
        [FromBody] UpdatePaddockRequest request
    )
    {
        try
        {
            var paddock = await mediator.Send(
                new UpdatePaddockCommand(
                    id,
                    request.Name,
                    farmId, // Use route farmId ensures consistency
                    request.Area,
                    request.AreaType
                )
            );

            return Ok(paddock);
        }
        catch (Exception ex)
        {
            return HandleServiceException(ex);
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "FarmAdminAccess")]
    public async Task<ActionResult> Delete(int farmId, int id)
    {
        try
        {
            await mediator.Send(new DeletePaddockCommand(id));
            return NoContent();
        }
        catch (Exception ex)
        {
            return HandleServiceException(ex);
        }
    }
}
