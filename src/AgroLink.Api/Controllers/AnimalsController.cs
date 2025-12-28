using AgroLink.Application.Features.Animals.Commands.Create;
using AgroLink.Application.Features.Animals.Commands.Delete;
using AgroLink.Application.Features.Animals.Commands.Move;
using AgroLink.Application.Features.Animals.Commands.Update;
using AgroLink.Application.Features.Animals.DTOs;
using AgroLink.Application.Features.Animals.Queries.GetAll;
using AgroLink.Application.Features.Animals.Queries.GetById;
using AgroLink.Application.Features.Animals.Queries.GetByLot;
using AgroLink.Application.Features.Animals.Queries.GetGenealogy;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace AgroLink.Api.Controllers;

[Route("api/[controller]")]
public class AnimalsController(IMediator mediator) : BaseController
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<AnimalDto>>> GetAll()
    {
        var animals = await mediator.Send(new GetAllAnimalsQuery());
        return Ok(animals);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<AnimalDto>> GetById(int id)
    {
        var animal = await mediator.Send(new GetAnimalByIdQuery(id));
        if (animal == null)
        {
            return NotFound();
        }

        return Ok(animal);
    }

    [HttpGet("lot/{lotId}")]
    public async Task<ActionResult<IEnumerable<AnimalDto>>> GetByLot(int lotId)
    {
        var animals = await mediator.Send(new GetAnimalsByLotQuery(lotId));
        return Ok(animals);
    }

    [HttpGet("{id}/genealogy")]
    public async Task<ActionResult<AnimalGenealogyDto>> GetGenealogy(int id)
    {
        var genealogy = await mediator.Send(new GetAnimalGenealogyQuery(id));
        if (genealogy == null)
        {
            return NotFound();
        }

        return Ok(genealogy);
    }

    [HttpPost]
    public async Task<ActionResult<AnimalDto>> Create(CreateAnimalDto dto)
    {
        try
        {
            var animal = await mediator.Send(new CreateAnimalCommand(dto));
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
            var animal = await mediator.Send(new UpdateAnimalCommand(id, dto));
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
            await mediator.Send(new DeleteAnimalCommand(id));
            return NoContent();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("{id}/move")]
    public async Task<ActionResult<AnimalDto>> MoveAnimal(
        int id,
        [FromBody] MoveAnimalRequest request
    )
    {
        try
        {
            var animal = await mediator.Send(
                new MoveAnimalCommand(id, request.FromLotId, request.ToLotId, request.Reason)
            );
            return Ok(animal);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}

public class MoveAnimalRequest
{
    public int FromLotId { get; set; }
    public int ToLotId { get; set; }
    public string? Reason { get; set; }
}
