using AgroLink.Application.Features.Paddocks.Commands.Create;
using AgroLink.Application.Features.Paddocks.Commands.Delete;
using AgroLink.Application.Features.Paddocks.Commands.Update;
using AgroLink.Application.Features.Paddocks.DTOs;
using AgroLink.Application.Features.Paddocks.Queries.GetAll;
using AgroLink.Application.Features.Paddocks.Queries.GetByFarm;
using AgroLink.Application.Features.Paddocks.Queries.GetById;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace AgroLink.Api.Controllers;

[Route("api/[controller]")]
public class PaddocksController(IMediator mediator) : BaseController
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<PaddockDto>>> GetAll()
    {
        var paddocks = await mediator.Send(new GetAllPaddocksQuery());
        return Ok(paddocks);
    }

    [HttpGet("farm/{farmId}")]
    public async Task<ActionResult<IEnumerable<PaddockDto>>> GetByFarm(int farmId)
    {
        var paddocks = await mediator.Send(new GetPaddocksByFarmQuery(farmId));
        return Ok(paddocks);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<PaddockDto>> GetById(int id)
    {
        var paddock = await mediator.Send(new GetPaddockByIdQuery(id));
        if (paddock == null)
        {
            return NotFound();
        }

        return Ok(paddock);
    }

    [HttpPost]
    public async Task<ActionResult<PaddockDto>> Create(CreatePaddockRequest request)
    {
        try
        {
            var dto = new CreatePaddockDto { Name = request.Name, FarmId = request.FarmId };
            var paddock = await mediator.Send(new CreatePaddockCommand(dto));
            return CreatedAtAction(nameof(GetById), new { id = paddock.Id }, paddock);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<PaddockDto>> Update(int id, UpdatePaddockRequest request)
    {
        try
        {
            var dto = new UpdatePaddockDto { Name = request.Name, FarmId = request.FarmId };
            var paddock = await mediator.Send(new UpdatePaddockCommand(id, dto));
            return Ok(paddock);
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
            await mediator.Send(new DeletePaddockCommand(id));
            return NoContent();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}

public class CreatePaddockRequest
{
    public string Name { get; set; } = string.Empty;
    public int FarmId { get; set; }
}

public class UpdatePaddockRequest
{
    public string? Name { get; set; }
    public int? FarmId { get; set; }
}
