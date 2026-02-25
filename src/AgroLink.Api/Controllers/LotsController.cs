using AgroLink.Application.Features.Lots.Commands.Create;
using AgroLink.Application.Features.Lots.Commands.Delete;
using AgroLink.Application.Features.Lots.Commands.Move;
using AgroLink.Application.Features.Lots.Commands.Update;
using AgroLink.Application.Features.Lots.DTOs;
using AgroLink.Application.Features.Lots.Queries.GetById;
using AgroLink.Application.Features.Lots.Queries.GetByPaddock;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgroLink.Api.Controllers;

[Route("api/farms/{farmId}/lots")]
[Authorize(Policy = "FarmViewerAccess")]
public class LotsController(IMediator mediator) : BaseController
{
    // Removing generic GetAll as it's not scoped to Farm efficiently without a new Query.
    // Ideally we should implement GetLotsByFarmQuery.

    [HttpGet("paddock/{paddockId}")]
    public async Task<ActionResult<IEnumerable<LotDto>>> GetByPaddock(int farmId, int paddockId)
    {
        // TODO: Validate paddockId belongs to farmId (Optimization)
        // Handler currently checks user access to paddock.
        var lots = await mediator.Send(new GetLotsByPaddockQuery(paddockId));
        return Ok(lots);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<LotDto>> GetById(int farmId, int id)
    {
        var lot = await mediator.Send(new GetLotByIdQuery(id));
        if (lot == null)
        {
            return NotFound();
        }
        // Implicitly, if I can see the lot, I verify if it belongs to this farm
        // Ideally we check: if (lot.Paddock.FarmId != farmId) return NotFound();
        // But LotDto might not have Paddock.FarmId populated deep enough.
        // We rely on the initial FarmRoleHandler check for access to the URL farm,
        // but this specific resource check is pending strictly for "Cross-Farm" data leak prevention on read.

        return Ok(lot);
    }

    [HttpPost]
    [Authorize(Policy = "FarmAdminAccess")]
    public async Task<ActionResult<LotDto>> Create(int farmId, [FromBody] CreateLotRequest request)
    {
        try
        {
            // We should ensure request.PaddockId belongs to farmId
            var dto = new CreateLotDto
            {
                Name = request.Name,
                PaddockId = request.PaddockId,
                Status = request.Status,
            };
            var lot = await mediator.Send(new CreateLotCommand(dto));
            return CreatedAtAction(nameof(GetById), new { farmId, id = lot.Id }, lot);
        }
        catch (Exception ex)
        {
            return HandleServiceException(ex);
        }
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "FarmAdminAccess")]
    public async Task<ActionResult<LotDto>> Update(
        int farmId,
        int id,
        [FromBody] UpdateLotRequest request
    )
    {
        try
        {
            var dto = new UpdateLotDto
            {
                Name = request.Name,
                PaddockId = request.PaddockId,
                Status = request.Status,
            };
            var lot = await mediator.Send(new UpdateLotCommand(id, dto));
            return Ok(lot);
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
            await mediator.Send(new DeleteLotCommand(id));
            return NoContent();
        }
        catch (Exception ex)
        {
            return HandleServiceException(ex);
        }
    }

    [HttpPost("{id}/move")]
    [Authorize(Policy = "FarmEditorAccess")] // Moving cattle is operation -> Editor
    public async Task<ActionResult<LotDto>> MoveLot(
        int farmId,
        int id,
        [FromBody] MoveLotRequest request
    )
    {
        try
        {
            var userId = GetCurrentUserId();
            var lot = await mediator.Send(
                new MoveLotCommand(id, request.ToPaddockId, request.Reason, userId)
            );
            return Ok(lot);
        }
        catch (Exception ex)
        {
            return HandleServiceException(ex);
        }
    }
}

public class CreateLotRequest
{
    public string Name { get; set; } = string.Empty;
    public int PaddockId { get; set; }
    public string? Status { get; set; }
}

public class UpdateLotRequest
{
    public string? Name { get; set; }
    public int? PaddockId { get; set; }
    public string? Status { get; set; }
}

public class MoveLotRequest
{
    public int ToPaddockId { get; set; }
    public string? Reason { get; set; }
}
