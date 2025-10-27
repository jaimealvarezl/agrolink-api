using AgroLink.Core.DTOs;
using AgroLink.Core.Entities;
using AgroLink.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgroLink.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PaddocksController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;

    public PaddocksController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<PaddockDto>>> GetAll()
    {
        var paddocks = await _unitOfWork.Paddocks.GetAllAsync();
        var result = new List<PaddockDto>();

        foreach (var paddock in paddocks)
        {
            var farm = await _unitOfWork.Farms.GetByIdAsync(paddock.FarmId);
            result.Add(new PaddockDto
            {
                Id = paddock.Id,
                Name = paddock.Name,
                FarmId = paddock.FarmId,
                FarmName = farm?.Name ?? "",
                CreatedAt = paddock.CreatedAt
            });
        }

        return Ok(result);
    }

    [HttpGet("farm/{farmId}")]
    public async Task<ActionResult<IEnumerable<PaddockDto>>> GetByFarm(int farmId)
    {
        var paddocks = await _unitOfWork.Paddocks.FindAsync(p => p.FarmId == farmId);
        var result = new List<PaddockDto>();

        foreach (var paddock in paddocks)
        {
            var farm = await _unitOfWork.Farms.GetByIdAsync(paddock.FarmId);
            result.Add(new PaddockDto
            {
                Id = paddock.Id,
                Name = paddock.Name,
                FarmId = paddock.FarmId,
                FarmName = farm?.Name ?? "",
                CreatedAt = paddock.CreatedAt
            });
        }

        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<PaddockDto>> GetById(int id)
    {
        var paddock = await _unitOfWork.Paddocks.GetByIdAsync(id);
        if (paddock == null)
            return NotFound();

        var farm = await _unitOfWork.Farms.GetByIdAsync(paddock.FarmId);
        var result = new PaddockDto
        {
            Id = paddock.Id,
            Name = paddock.Name,
            FarmId = paddock.FarmId,
            FarmName = farm?.Name ?? "",
            CreatedAt = paddock.CreatedAt
        };

        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<PaddockDto>> Create(CreatePaddockRequest request)
    {
        var paddock = new Paddock
        {
            Name = request.Name,
            FarmId = request.FarmId
        };

        await _unitOfWork.Paddocks.AddAsync(paddock);
        await _unitOfWork.SaveChangesAsync();

        var farm = await _unitOfWork.Farms.GetByIdAsync(paddock.FarmId);
        var result = new PaddockDto
        {
            Id = paddock.Id,
            Name = paddock.Name,
            FarmId = paddock.FarmId,
            FarmName = farm?.Name ?? "",
            CreatedAt = paddock.CreatedAt
        };

        return CreatedAtAction(nameof(GetById), new { id = paddock.Id }, result);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<PaddockDto>> Update(int id, UpdatePaddockRequest request)
    {
        var paddock = await _unitOfWork.Paddocks.GetByIdAsync(id);
        if (paddock == null)
            return NotFound();

        paddock.Name = request.Name ?? paddock.Name;
        paddock.FarmId = request.FarmId ?? paddock.FarmId;
        paddock.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Paddocks.UpdateAsync(paddock);
        await _unitOfWork.SaveChangesAsync();

        var farm = await _unitOfWork.Farms.GetByIdAsync(paddock.FarmId);
        var result = new PaddockDto
        {
            Id = paddock.Id,
            Name = paddock.Name,
            FarmId = paddock.FarmId,
            FarmName = farm?.Name ?? "",
            CreatedAt = paddock.CreatedAt
        };

        return Ok(result);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        var paddock = await _unitOfWork.Paddocks.GetByIdAsync(id);
        if (paddock == null)
            return NotFound();

        await _unitOfWork.Paddocks.DeleteAsync(paddock);
        await _unitOfWork.SaveChangesAsync();

        return NoContent();
    }
}

public class CreatePaddockRequest
{
    public string Name { get; set; } = string.Empty;
    public int FarmId { get; set; }
}

public class UpdatePaddockRequest
{
    public string? Name { get; set; }
    public int? FarmId { get; set; }
}