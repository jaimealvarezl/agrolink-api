using AgroLink.Api.DTOs.Paddocks;
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
            var userId = GetCurrentUserId();
            var paddock = await mediator.Send(
                new CreatePaddockCommand(request.Name, request.FarmId, userId)
            );
            return CreatedAtAction(nameof(GetById), new { id = paddock.Id }, paddock);
        }
        catch (Exception ex)
        {
            return HandleServiceException(ex);
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<PaddockDto>> Update(int id, UpdatePaddockRequest request)
    {
        try
        {
            var paddock = await mediator.Send(
                new UpdatePaddockCommand(id, request.Name, request.FarmId)
            );
            return Ok(paddock);
        }
        catch (Exception ex)
        {
            return HandleServiceException(ex);
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
        catch (Exception ex)
        {
            return HandleServiceException(ex);
        }
    }
}
