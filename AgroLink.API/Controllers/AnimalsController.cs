using AgroLink.Core.DTOs;
using AgroLink.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgroLink.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AnimalsController : ControllerBase
{
    private readonly IAnimalService _animalService;

    public AnimalsController(IAnimalService animalService)
    {
        _animalService = animalService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<AnimalDto>>> GetAll()
    {
        var animals = await _animalService.GetAllAsync();
        return Ok(animals);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<AnimalDto>> GetById(int id)
    {
        var animal = await _animalService.GetByIdAsync(id);
        if (animal == null)
            return NotFound();

        return Ok(animal);
    }

    [HttpGet("lot/{lotId}")]
    public async Task<ActionResult<IEnumerable<AnimalDto>>> GetByLot(int lotId)
    {
        var animals = await _animalService.GetByLotAsync(lotId);
        return Ok(animals);
    }

    [HttpGet("{id}/genealogy")]
    public async Task<ActionResult<AnimalGenealogyDto>> GetGenealogy(int id)
    {
        var genealogy = await _animalService.GetGenealogyAsync(id);
        if (genealogy == null)
            return NotFound();

        return Ok(genealogy);
    }

    [HttpPost]
    public async Task<ActionResult<AnimalDto>> Create(CreateAnimalDto dto)
    {
        try
        {
            var animal = await _animalService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = animal.Id }, animal);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<AnimalDto>> Update(int id, UpdateAnimalDto dto)
    {
        try
        {
            var animal = await _animalService.UpdateAsync(id, dto);
            return Ok(animal);
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
            await _animalService.DeleteAsync(id);
            return NoContent();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("{id}/move")]
    public async Task<ActionResult<AnimalDto>> MoveAnimal(int id, [FromBody] MoveAnimalRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            var animal = await _animalService.MoveAnimalAsync(id, request.FromLotId, request.ToLotId, request.Reason, userId);
            return Ok(animal);
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

public class MoveAnimalRequest
{
    public int FromLotId { get; set; }
    public int ToLotId { get; set; }
    public string? Reason { get; set; }
}