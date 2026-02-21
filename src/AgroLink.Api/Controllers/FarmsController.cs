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
        try
        {
            var farms = await mediator.Send(new GetAllFarmsQuery());
            return Ok(farms);
        }
        catch (Exception ex)
        {
            return HandleServiceException(ex);
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<FarmDto>> GetById(int id)
    {
        try
        {
            var farm = await mediator.Send(new GetFarmByIdQuery(id));
            if (farm == null)
            {
                return NotFound();
            }

            return Ok(farm);
        }
        catch (Exception ex)
        {
            return HandleServiceException(ex);
        }
    }

    [HttpGet("{id}/hierarchy")]
    public async Task<ActionResult<FarmHierarchyDto>> GetHierarchy(int id)
    {
        try
        {
            var userId = GetCurrentUserId();
            var farm = await mediator.Send(new GetFarmHierarchyQuery(id, userId));
            if (farm == null)
            {
                return NotFound();
            }

            return Ok(farm);
        }
        catch (Exception ex)
        {
            return HandleServiceException(ex);
        }
    }

    [HttpPost]
    public async Task<ActionResult<FarmDto>> Create(CreateFarmRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            var farm = await mediator.Send(
                new CreateFarmCommand(request.Name, request.Location, request.CUE, userId)
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
                new UpdateFarmCommand(id, request.Name, request.Location, request.CUE)
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
            var userId = GetCurrentUserId();
            await mediator.Send(new DeleteFarmCommand(id, userId));
            return NoContent();
        }
        catch (Exception ex)
        {
            return HandleServiceException(ex);
        }
    }
}
