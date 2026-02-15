using AgroLink.Application.Common.Utilities;
using AgroLink.Application.Features.Animals.Commands.Create;
using AgroLink.Application.Features.Animals.Commands.Delete;
using AgroLink.Application.Features.Animals.Commands.DeletePhoto;
using AgroLink.Application.Features.Animals.Commands.Move;
using AgroLink.Application.Features.Animals.Commands.SetProfilePhoto;
using AgroLink.Application.Features.Animals.Commands.Update;
using AgroLink.Application.Features.Animals.Commands.UploadPhoto;
using AgroLink.Application.Features.Animals.DTOs;
using AgroLink.Application.Features.Animals.Queries.GetAll;
using AgroLink.Application.Features.Animals.Queries.GetById;
using AgroLink.Application.Features.Animals.Queries.GetByLot;
using AgroLink.Application.Features.Animals.Queries.GetColors;
using AgroLink.Application.Features.Animals.Queries.GetDetail;
using AgroLink.Application.Features.Animals.Queries.GetGenealogy;
using AgroLink.Application.Features.Animals.Queries.GetPagedList;
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

    [HttpGet("colors")]
    public async Task<ActionResult<IEnumerable<string>>> GetColors()
    {
        var colors = await mediator.Send(new GetAnimalColorsQuery(GetCurrentUserId()));
        return Ok(colors);
    }

    [HttpGet("search")]
    public async Task<ActionResult<PagedResult<AnimalListDto>>> GetPagedList(
        [FromQuery] GetAnimalsPagedListQuery query
    )
    {
        var result = await mediator.Send(query);
        return Ok(result);
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

    [HttpGet("{id}/details")]
    public async Task<ActionResult<AnimalDetailDto>> GetDetail(int id)
    {
        var animal = await mediator.Send(new GetAnimalDetailQuery(id));
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
        catch (Exception ex)
        {
            return HandleServiceException(ex);
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
        catch (Exception ex)
        {
            return HandleServiceException(ex);
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
        catch (Exception ex)
        {
            return HandleServiceException(ex);
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
        catch (Exception ex)
        {
            return HandleServiceException(ex);
        }
    }

    [HttpPost("{id}/photos")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<AnimalPhotoDto>> UploadPhoto(
        int id,
        IFormFile file,
        [FromForm] string? description
    )
    {
        try
        {
            await using var stream = file.OpenReadStream();
            var command = new UploadAnimalPhotoCommand(
                id,
                stream,
                file.FileName,
                file.ContentType,
                file.Length,
                description
            );

            var result = await mediator.Send(command);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return HandleServiceException(ex);
        }
    }

    [HttpPut("{id}/photos/{photoId}/profile")]
    public async Task<ActionResult> SetProfilePhoto(int id, int photoId)
    {
        try
        {
            await mediator.Send(new SetAnimalProfilePhotoCommand(id, photoId));
            return NoContent();
        }
        catch (Exception ex)
        {
            return HandleServiceException(ex);
        }
    }

    [HttpDelete("{id}/photos/{photoId}")]
    public async Task<ActionResult> DeletePhoto(int id, int photoId)
    {
        try
        {
            await mediator.Send(new DeleteAnimalPhotoCommand(id, photoId));
            return NoContent();
        }
        catch (Exception ex)
        {
            return HandleServiceException(ex);
        }
    }
}

public class MoveAnimalRequest
{
    public int FromLotId { get; set; }
    public int ToLotId { get; set; }
    public string? Reason { get; set; }
}
