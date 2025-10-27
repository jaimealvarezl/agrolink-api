using AgroLink.Core.DTOs;
using AgroLink.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgroLink.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MovementsController(IMovementService movementService) : ControllerBase
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

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst("userid");
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            throw new UnauthorizedAccessException("Invalid user token");
        return userId;
    }
}
