using AgroLink.Application.DTOs;
using AgroLink.Application.Features.Movements.Commands.CreateMovement;
using AgroLink.Application.Features.Movements.Queries.GetMovementsByEntity;
using MediatR;
using Microsoft.AspNetCore.Mvc;

// Added this using directive

namespace AgroLink.Api.Controllers;

[Route("api/[controller]")]
public class MovementsController(IMediator mediator) : BaseController
{
    [HttpGet("entity/{entityType}/{entityId}")]
    public async Task<ActionResult<IEnumerable<MovementDto>>> GetByEntity(
        string entityType,
        int entityId
    )
    {
        var movements = await mediator.Send(new GetMovementsByEntityQuery(entityType, entityId));
        return Ok(movements);
    }

    [HttpGet("animal/{animalId}/history")]
    public async Task<ActionResult<IEnumerable<MovementDto>>> GetAnimalHistory(int animalId)
    {
        var movements = await mediator.Send(new GetMovementsByEntityQuery("ANIMAL", animalId));
        return Ok(movements);
    }

    [HttpPost]
    public async Task<ActionResult<MovementDto>> Create(CreateMovementDto dto)
    {
        try
        {
            var userId = GetCurrentUserId();
            var movement = await mediator.Send(new CreateMovementCommand(dto, userId));
            return CreatedAtAction(
                nameof(GetByEntity),
                new { entityType = dto.EntityType, entityId = dto.EntityId },
                movement
            );
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}
