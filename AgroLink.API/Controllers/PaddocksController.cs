using AgroLink.Core.DTOs;
using AgroLink.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgroLink.API.Controllers;

[Route("api/[controller]")]
public class PaddocksController(IPaddockService paddockService) : BaseController
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<PaddockDto>>> GetAll()
    {
        var paddocks = await paddockService.GetAllAsync();
        return Ok(paddocks);
    }

    [HttpGet("farm/{farmId}")]
    public async Task<ActionResult<IEnumerable<PaddockDto>>> GetByFarm(int farmId)
    {
        var paddocks = await paddockService.GetByFarmAsync(farmId);
        return Ok(paddocks);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<PaddockDto>> GetById(int id)
    {
        var paddock = await paddockService.GetByIdAsync(id);
        if (paddock == null)
            return NotFound();

        return Ok(paddock);
    }

    [HttpPost]
    public async Task<ActionResult<PaddockDto>> Create(CreatePaddockRequest request)
    {
        try
        {
            var dto = new CreatePaddockDto { Name = request.Name, FarmId = request.FarmId };
            var paddock = await paddockService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = paddock.Id }, paddock);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<PaddockDto>> Update(int id, UpdatePaddockRequest request)
    {
        try
        {
            var dto = new UpdatePaddockDto { Name = request.Name, FarmId = request.FarmId };
            var paddock = await paddockService.UpdateAsync(id, dto);
            return Ok(paddock);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        try
        {
            await paddockService.DeleteAsync(id);
            return NoContent();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
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