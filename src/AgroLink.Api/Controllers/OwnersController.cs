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
    [Authorize(Policy = "FarmAdminAccess")] // Owner or Admin
    public async Task<ActionResult<IEnumerable<OwnerDto>>> GetOwners(int farmId)
    {
        var owners = await mediator.Send(new GetOwnersByFarmIdQuery(farmId));
        return Ok(owners);
    }

    [HttpPost]
    [Authorize(Policy = "FarmAdminAccess")] // Owner or Admin
    public async Task<ActionResult<OwnerDto>> Create(int farmId, CreateOwnerRequest request)
    {
        var owner = await mediator.Send(
            new CreateOwnerCommand(
                farmId,
                request.Name,
                request.Phone,
                request.Email,
                request.UserId
            )
        );
        return CreatedAtAction(nameof(GetOwners), new { farmId }, owner);
    }

    [HttpDelete("{ownerId}")]
    [Authorize(Policy = "FarmOwnerOnly")] // Owner only
    public async Task<ActionResult> Delete(int farmId, int ownerId)
    {
        await mediator.Send(new DeleteOwnerCommand(farmId, ownerId));
        return NoContent();
    }
}
