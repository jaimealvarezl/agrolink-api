using AgroLink.Core.DTOs;
using AgroLink.Core.Entities;
using AgroLink.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AgroLink.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LotsController : ControllerBase
{
    private readonly AgroLinkDbContext _context;

    public LotsController(AgroLinkDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<LotDto>>> GetAll()
    {
        var lots = await _context.Lots.ToListAsync();
        var result = new List<LotDto>();

        foreach (var lot in lots)
        {
            var paddock = await _context.Paddocks.FindAsync(lot.PaddockId);
            result.Add(
                new LotDto
                {
                    Id = lot.Id,
                    Name = lot.Name,
                    PaddockId = lot.PaddockId,
                    PaddockName = paddock?.Name ?? "",
                    Status = lot.Status,
                    CreatedAt = lot.CreatedAt,
                }
            );
        }

        return Ok(result);
    }

    [HttpGet("paddock/{paddockId}")]
    public async Task<ActionResult<IEnumerable<LotDto>>> GetByPaddock(int paddockId)
    {
        var lots = await _context.Lots.Where(l => l.PaddockId == paddockId).ToListAsync();
        var result = new List<LotDto>();

        foreach (var lot in lots)
        {
            var paddock = await _context.Paddocks.FindAsync(lot.PaddockId);
            result.Add(
                new LotDto
                {
                    Id = lot.Id,
                    Name = lot.Name,
                    PaddockId = lot.PaddockId,
                    PaddockName = paddock?.Name ?? "",
                    Status = lot.Status,
                    CreatedAt = lot.CreatedAt,
                }
            );
        }

        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<LotDto>> GetById(int id)
    {
        var lot = await _context.Lots.FindAsync(id);
        if (lot == null)
            return NotFound();

        var paddock = await _context.Paddocks.FindAsync(lot.PaddockId);
        var result = new LotDto
        {
            Id = lot.Id,
            Name = lot.Name,
            PaddockId = lot.PaddockId,
            PaddockName = paddock?.Name ?? "",
            Status = lot.Status,
            CreatedAt = lot.CreatedAt,
        };

        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<LotDto>> Create(CreateLotRequest request)
    {
        var lot = new Lot
        {
            Name = request.Name,
            PaddockId = request.PaddockId,
            Status = request.Status ?? "ACTIVE",
        };

        _context.Lots.Add(lot);
        await _context.SaveChangesAsync();

        var paddock = await _context.Paddocks.FindAsync(lot.PaddockId);
        var result = new LotDto
        {
            Id = lot.Id,
            Name = lot.Name,
            PaddockId = lot.PaddockId,
            PaddockName = paddock?.Name ?? "",
            Status = lot.Status,
            CreatedAt = lot.CreatedAt,
        };

        return CreatedAtAction(nameof(GetById), new { id = lot.Id }, result);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<LotDto>> Update(int id, UpdateLotRequest request)
    {
        var lot = await _context.Lots.FindAsync(id);
        if (lot == null)
            return NotFound();

        lot.Name = request.Name ?? lot.Name;
        lot.PaddockId = request.PaddockId ?? lot.PaddockId;
        lot.Status = request.Status ?? lot.Status;
        lot.UpdatedAt = DateTime.UtcNow;

        _context.Lots.Update(lot);
        await _context.SaveChangesAsync();

        var paddock = await _context.Paddocks.FindAsync(lot.PaddockId);
        var result = new LotDto
        {
            Id = lot.Id,
            Name = lot.Name,
            PaddockId = lot.PaddockId,
            PaddockName = paddock?.Name ?? "",
            Status = lot.Status,
            CreatedAt = lot.CreatedAt,
        };

        return Ok(result);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        var lot = await _context.Lots.FindAsync(id);
        if (lot == null)
            return NotFound();

        _context.Lots.Remove(lot);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpPost("{id}/move")]
    public async Task<ActionResult<LotDto>> MoveLot(int id, [FromBody] MoveLotRequest request)
    {
        var lot = await _context.Lots.FindAsync(id);
        if (lot == null)
            return NotFound();

        var fromPaddockId = lot.PaddockId;
        lot.PaddockId = request.ToPaddockId;
        lot.UpdatedAt = DateTime.UtcNow;

        _context.Lots.Update(lot);

        // Record movement
        var userId = GetCurrentUserId();
        var movement = new Movement
        {
            EntityType = "LOT",
            EntityId = id,
            FromId = fromPaddockId,
            ToId = request.ToPaddockId,
            At = DateTime.UtcNow,
            Reason = request.Reason,
            UserId = userId,
        };

        _context.Movements.Add(movement);
        await _context.SaveChangesAsync();

        var paddock = await _context.Paddocks.FindAsync(lot.PaddockId);
        var result = new LotDto
        {
            Id = lot.Id,
            Name = lot.Name,
            PaddockId = lot.PaddockId,
            PaddockName = paddock?.Name ?? "",
            Status = lot.Status,
            CreatedAt = lot.CreatedAt,
        };

        return Ok(result);
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst("userid");
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            throw new UnauthorizedAccessException("Invalid user token");
        return userId;
    }
}

public class CreateLotRequest
{
    public string Name { get; set; } = string.Empty;
    public int PaddockId { get; set; }
    public string? Status { get; set; }
}

public class UpdateLotRequest
{
    public string? Name { get; set; }
    public int? PaddockId { get; set; }
    public string? Status { get; set; }
}

public class MoveLotRequest
{
    public int ToPaddockId { get; set; }
    public string? Reason { get; set; }
}
