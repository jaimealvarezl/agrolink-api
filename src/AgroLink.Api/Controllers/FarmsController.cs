using AgroLink.Api.DTOs.Farms;
using AgroLink.Application.Features.Farms.Commands.Create;
using AgroLink.Application.Features.Farms.Commands.Delete;
using AgroLink.Application.Features.Farms.Commands.Update;
using AgroLink.Application.Features.Farms.DTOs;
using AgroLink.Application.Features.Farms.Queries.GetAll;
using AgroLink.Application.Features.Farms.Queries.GetById;
using AgroLink.Application.Features.Farms.Queries.GetFarmHierarchy;
using MediatR;
using Microsoft.AspNetCore.Authorization;
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
            var userId = GetCurrentUserId();
            var farms = await mediator.Send(new GetAllFarmsQuery(userId));
            return Ok(farms);
        }
        catch (Exception ex)
        {
            return HandleServiceException(ex);
        }
    }

    [HttpGet("{farmId}")]
    [Authorize(Policy = "FarmViewerAccess")]
    public async Task<ActionResult<FarmDto>> GetById(int farmId)
    {
        try
        {
            var userId = GetCurrentUserId();
            var farm = await mediator.Send(new GetFarmByIdQuery(farmId, userId));
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

    [HttpGet("{farmId}/hierarchy")]
    [Authorize(Policy = "FarmViewerAccess")]
    public async Task<ActionResult<FarmHierarchyDto>> GetHierarchy(int farmId)
    {
        try
        {
            var userId = GetCurrentUserId();
            var farm = await mediator.Send(new GetFarmHierarchyQuery(farmId, userId));
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
            return CreatedAtAction(nameof(GetById), new { farmId = farm.Id }, farm);
        }
        catch (Exception ex)
        {
            return HandleServiceException(ex);
        }
    }

    [HttpPut("{farmId}")]
    [Authorize(Policy = "FarmOwnerOnly")]
    public async Task<ActionResult<FarmDto>> Update(int farmId, UpdateFarmRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            var farm = await mediator.Send(
                new UpdateFarmCommand(farmId, request.Name, request.Location, request.CUE, userId)
            );
            return Ok(farm);
        }
        catch (Exception ex)
        {
            return HandleServiceException(ex);
        }
    }

    [HttpDelete("{farmId}")]
    [Authorize(Policy = "FarmOwnerOnly")]
    public async Task<ActionResult> Delete(int farmId)
    {
        try
        {
            var userId = GetCurrentUserId();
            await mediator.Send(new DeleteFarmCommand(farmId, userId));
            return NoContent();
        }
        catch (Exception ex)
        {
            return HandleServiceException(ex);
        }
    }
}
