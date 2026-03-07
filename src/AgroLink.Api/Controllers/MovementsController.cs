using AgroLink.Application.Features.Movements.Commands.CreateMovement;
using AgroLink.Application.Features.Movements.DTOs;
using AgroLink.Application.Features.Movements.Queries.GetMovementsByEntity;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgroLink.Api.Controllers;

[Route("api/farms/{farmId}/movements")]
[Authorize(Policy = "FarmViewerAccess")]
public class MovementsController(IMediator mediator) : BaseController
{
    [HttpGet("entity/{entityType}/{entityId}")]
    public async Task<ActionResult<IEnumerable<MovementDto>>> GetByEntity(
        int farmId,
        string entityType,
        int entityId
    )
    {
        // Ideally we should validate entityId belongs to farmId
        var movements = await mediator.Send(new GetMovementsByEntityQuery(entityType, entityId));
        return Ok(movements);
    }

    [HttpGet("animal/{animalId}/history")]
    public async Task<ActionResult<IEnumerable<MovementDto>>> GetAnimalHistory(
        int farmId,
        int animalId
    )
    {
        var movements = await mediator.Send(new GetMovementsByEntityQuery("ANIMAL", animalId));
        return Ok(movements);
    }

    [HttpPost]
    [Authorize(Policy = "FarmEditorAccess")]
    public async Task<ActionResult<IEnumerable<MovementDto>>> Create(
        int farmId,
        [FromBody] CreateMovementDto dto
    )
    {
        var userId = GetCurrentUserId();
        var movements = await mediator.Send(new CreateMovementCommand(dto, userId));
        return Ok(movements);
    }
}
