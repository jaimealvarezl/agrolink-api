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
public class FarmsController : ControllerBase
{
    private readonly AgroLinkDbContext _context;

    public FarmsController(AgroLinkDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<FarmDto>>> GetAll()
    {
        var farms = await _context.Farms.ToListAsync();
        var result = farms.Select(f => new FarmDto
        {
            Id = f.Id,
            Name = f.Name,
            Location = f.Location,
            CreatedAt = f.CreatedAt
        });

        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<FarmDto>> GetById(int id)
    {
        var farm = await _context.Farms.FindAsync(id);
        if (farm == null)
            return NotFound();

        var result = new FarmDto
        {
            Id = farm.Id,
            Name = farm.Name,
            Location = farm.Location,
            CreatedAt = farm.CreatedAt
        };

        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<FarmDto>> Create(CreateFarmRequest request)
    {
        var farm = new Farm
        {
            Name = request.Name,
            Location = request.Location
        };

        _context.Farms.Add(farm);
        await _context.SaveChangesAsync();

        var result = new FarmDto
        {
            Id = farm.Id,
            Name = farm.Name,
            Location = farm.Location,
            CreatedAt = farm.CreatedAt
        };

        return CreatedAtAction(nameof(GetById), new { id = farm.Id }, result);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<FarmDto>> Update(int id, UpdateFarmRequest request)
    {
        var farm = await _context.Farms.FindAsync(id);
        if (farm == null)
            return NotFound();

        farm.Name = request.Name ?? farm.Name;
        farm.Location = request.Location ?? farm.Location;
        farm.UpdatedAt = DateTime.UtcNow;

        _context.Farms.Update(farm);
        await _context.SaveChangesAsync();

        var result = new FarmDto
        {
            Id = farm.Id,
            Name = farm.Name,
            Location = farm.Location,
            CreatedAt = farm.CreatedAt
        };

        return Ok(result);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        var farm = await _context.Farms.FindAsync(id);
        if (farm == null)
            return NotFound();

        _context.Farms.Remove(farm);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}

public class CreateFarmRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Location { get; set; }
}

public class UpdateFarmRequest
{
    public string? Name { get; set; }
    public string? Location { get; set; }
}