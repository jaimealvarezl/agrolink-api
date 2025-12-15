using AgroLink.Application.DTOs;
using AgroLink.Application.Interfaces;
using AgroLink.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AgroLink.Api.Controllers;

[Route("api/[controller]")]
public class MovementsController(IMovementService movementService) : BaseController
{
    [HttpGet("entity/{entityType}/{entityId}")]
    public async Task<ActionResult<IEnumerable<MovementDto>>> GetByEntity(
        string entityType,
        int entityId
    )
    {
        var movements = await movementService.GetByEntityAsync(entityType, entityId);
        return Ok(movements);
    }

    [HttpGet("animal/{animalId}/history")]
    public async Task<ActionResult<IEnumerable<MovementDto>>> GetAnimalHistory(int animalId)
    {
        var movements = await movementService.GetAnimalHistoryAsync(animalId);
        return Ok(movements);
    }

    [HttpPost]
    public async Task<ActionResult<MovementDto>> Create(CreateMovementDto dto)
    {
        try
        {
            var userId = GetCurrentUserId();
            var movement = await movementService.CreateAsync(dto, userId);
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
