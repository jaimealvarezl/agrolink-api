using AgroLink.Api.DTOs.Farms;
using AgroLink.Application.Features.Farms.Commands.Create;
using AgroLink.Application.Features.Farms.Commands.Delete;
using AgroLink.Application.Features.Farms.Commands.Update;
using AgroLink.Application.Features.Farms.DTOs;
using AgroLink.Application.Features.Farms.Queries.GetAll;
using AgroLink.Application.Features.Farms.Queries.GetById;
using AgroLink.Application.Features.Farms.Queries.GetFarmHierarchy;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace AgroLink.Api.Controllers;

[Route("api/[controller]")]
public class FarmsController(IMediator mediator) : BaseController
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<FarmDto>>> GetAll()
    {
        var farms = await mediator.Send(new GetAllFarmsQuery());
        return Ok(farms);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<FarmDto>> GetById(int id)
    {
        var farm = await mediator.Send(new GetFarmByIdQuery(id));
        if (farm == null)
        {
            return NotFound();
        }

        return Ok(farm);
    }

    [HttpGet("{id}/hierarchy")]
    public async Task<ActionResult<FarmHierarchyDto>> GetHierarchy(int id)
    {
        var farm = await mediator.Send(new GetFarmHierarchyQuery(id));
        if (farm == null)
        {
            return NotFound();
        }

        return Ok(farm);
    }

    [HttpPost]
    public async Task<ActionResult<FarmDto>> Create(CreateFarmRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            var farm = await mediator.Send(
                new CreateFarmCommand(request.Name, request.Location, userId)
            );
            return CreatedAtAction(nameof(GetById), new { id = farm.Id }, farm);
        }
        catch (Exception ex)
        {
            return HandleServiceException(ex);
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<FarmDto>> Update(int id, UpdateFarmRequest request)
    {
        try
        {
            var farm = await mediator.Send(
                new UpdateFarmCommand(id, request.Name, request.Location)
            );
            return Ok(farm);
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
            await mediator.Send(new DeleteFarmCommand(id));
            return NoContent();
        }
        catch (Exception ex)
        {
            return HandleServiceException(ex);
        }
    }
}
