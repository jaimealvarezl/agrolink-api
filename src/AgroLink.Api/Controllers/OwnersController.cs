using AgroLink.Api.DTOs.Owners;
using AgroLink.Application.Features.Owners.Commands.Create;
using AgroLink.Application.Features.Owners.Commands.Delete;
using AgroLink.Application.Features.Owners.DTOs;
using AgroLink.Application.Features.Owners.Queries.GetByFarm;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgroLink.Api.Controllers;

[Route("api/farms/{farmId}/owners")]
public class OwnersController(IMediator mediator) : BaseController
{
    [HttpGet]
    [Authorize(Policy = "FarmAdminAccess")]
    public async Task<ActionResult<IEnumerable<OwnerDto>>> GetOwners(int farmId)
    {
        try
        {
            var userId = GetCurrentUserId();
            var owners = await mediator.Send(new GetOwnersByFarmIdQuery(farmId, userId));
            return Ok(owners);
        }
        catch (Exception ex)
        {
            return HandleServiceException(ex);
        }
    }

    [HttpPost]
    [Authorize(Policy = "FarmAdminAccess")] // Owner or Admin
    public async Task<ActionResult<OwnerDto>> Create(int farmId, CreateOwnerRequest request)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            var owner = await mediator.Send(
                new CreateOwnerCommand(
                    farmId,
                    request.Name,
                    request.Phone,
                    request.Email,
                    request.UserId,
                    currentUserId
                )
            );
            return CreatedAtAction(nameof(GetOwners), new { farmId }, owner);
        }
        catch (Exception ex)
        {
            return HandleServiceException(ex);
        }
    }

    [HttpDelete("{ownerId}")]
    [Authorize(Policy = "FarmOwnerOnly")] // Owner only
    public async Task<ActionResult> Delete(int farmId, int ownerId)
    {
        try
        {
            var userId = GetCurrentUserId();
            await mediator.Send(new DeleteOwnerCommand(farmId, ownerId, userId));
            return NoContent();
        }
        catch (Exception ex)
        {
            return HandleServiceException(ex);
        }
    }
}
