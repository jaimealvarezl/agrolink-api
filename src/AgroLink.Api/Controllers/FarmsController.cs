using System.ComponentModel.DataAnnotations;
using AgroLink.Application.Features.Farms.Commands.Create;
using AgroLink.Application.Features.Farms.Commands.Delete;
using AgroLink.Application.Features.Farms.Commands.Update;
using AgroLink.Application.Features.Farms.DTOs;
using AgroLink.Application.Features.Farms.Queries.GetAll;
using AgroLink.Application.Features.Farms.Queries.GetById;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace AgroLink.Api.Controllers;

[Route("api/[controller]")]
public class FarmsController(IMediator mediator) : BaseController
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<FarmDto>>> GetAll()
    {
        var farms = await mediator.Send(new GetAllFarmsQuery());
        return Ok(farms);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<FarmDto>> GetById(int id)
    {
        var farm = await mediator.Send(new GetFarmByIdQuery(id));
        if (farm == null)
        {
            return NotFound();
        }

        return Ok(farm);
    }

    [HttpPost]
    public async Task<ActionResult<FarmDto>> Create(CreateFarmRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            var dto = new CreateFarmDto { Name = request.Name, Location = request.Location };
            var farm = await mediator.Send(new CreateFarmCommand(dto, userId));
            return CreatedAtAction(nameof(GetById), new { id = farm.Id }, farm);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<FarmDto>> Update(int id, UpdateFarmRequest request)
    {
        try
        {
            var dto = new UpdateFarmDto { Name = request.Name, Location = request.Location };
            var farm = await mediator.Send(new UpdateFarmCommand(id, dto));
            return Ok(farm);
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
            await mediator.Send(new DeleteFarmCommand(id));
            return NoContent();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}

public class CreateFarmRequest
{
    [Required]
    public string Name { get; set; } = string.Empty;
    public string? Location { get; set; }
}

public class UpdateFarmRequest
{
    public string? Name { get; set; }
    public string? Location { get; set; }
}
