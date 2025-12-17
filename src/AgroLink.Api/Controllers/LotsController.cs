using AgroLink.Application.Features.Lots.Commands.Create;
using AgroLink.Application.Features.Lots.Commands.Delete;
using AgroLink.Application.Features.Lots.Commands.Move;
using AgroLink.Application.Features.Lots.Commands.Update;
using AgroLink.Application.Features.Lots.DTOs;
using AgroLink.Application.Features.Lots.Queries.GetAll;
using AgroLink.Application.Features.Lots.Queries.GetById;
using AgroLink.Application.Features.Lots.Queries.GetByPaddock;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace AgroLink.Api.Controllers;

[Route("api/[controller]")]
public class LotsController(IMediator mediator) : BaseController
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<LotDto>>> GetAll()
    {
        var lots = await mediator.Send(new GetAllLotsQuery());
        return Ok(lots);
    }

    [HttpGet("paddock/{paddockId}")]
    public async Task<ActionResult<IEnumerable<LotDto>>> GetByPaddock(int paddockId)
    {
        var lots = await mediator.Send(new GetLotsByPaddockQuery(paddockId));
        return Ok(lots);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<LotDto>> GetById(int id)
    {
        var lot = await mediator.Send(new GetLotByIdQuery(id));
        if (lot == null)
        {
            return NotFound();
        }

        return Ok(lot);
    }

    [HttpPost]
    public async Task<ActionResult<LotDto>> Create(CreateLotRequest request)
    {
        try
        {
            var dto = new CreateLotDto
            {
                Name = request.Name,
                PaddockId = request.PaddockId,
                Status = request.Status,
            };
            var lot = await mediator.Send(new CreateLotCommand(dto));
            return CreatedAtAction(nameof(GetById), new { id = lot.Id }, lot);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<LotDto>> Update(int id, UpdateLotRequest request)
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
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        try
        {
            await mediator.Send(new DeleteLotCommand(id));
            return NoContent();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("{id}/move")]
    public async Task<ActionResult<LotDto>> MoveLot(int id, [FromBody] MoveLotRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            var lot = await mediator.Send(
                new MoveLotCommand(id, request.ToPaddockId, request.Reason, userId)
            );
            return Ok(lot);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
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
