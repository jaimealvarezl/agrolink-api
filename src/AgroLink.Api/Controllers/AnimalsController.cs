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
    public async Task<ActionResult<IEnumerable<AnimalDto>>> GetAll(
        CancellationToken cancellationToken
    )
    {
        var animals = await mediator.Send(new GetAllAnimalsQuery(GetCurrentUserId()), cancellationToken);
        return Ok(animals);
    }

    [HttpGet("colors")]
    public async Task<ActionResult<IEnumerable<string>>> GetColors(
        CancellationToken cancellationToken
    )
    {
        var colors = await mediator.Send(
            new GetAnimalColorsQuery(GetCurrentUserId()),
            cancellationToken
        );
        return Ok(colors);
    }

    [HttpGet("search")]
    public async Task<ActionResult<PagedResult<AnimalListDto>>> GetPagedList(
        [FromQuery] GetAnimalsPagedListQuery query,
        CancellationToken cancellationToken
    )
    {
        var result = await mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<AnimalDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var animal = await mediator.Send(new GetAnimalByIdQuery(id), cancellationToken);
        if (animal == null)
        {
            return NotFound();
        }

        return Ok(animal);
    }

    [HttpGet("{id}/details")]
    public async Task<ActionResult<AnimalDetailDto>> GetDetail(
        int id,
        CancellationToken cancellationToken
    )
    {
        var animal = await mediator.Send(new GetAnimalDetailQuery(id), cancellationToken);
        if (animal == null)
        {
            return NotFound();
        }

        return Ok(animal);
    }

    [HttpGet("lot/{lotId}")]
    public async Task<ActionResult<IEnumerable<AnimalDto>>> GetByLot(
        int lotId,
        CancellationToken cancellationToken
    )
    {
        var animals = await mediator.Send(new GetAnimalsByLotQuery(lotId), cancellationToken);
        return Ok(animals);
    }

    [HttpGet("{id}/genealogy")]
    public async Task<ActionResult<AnimalGenealogyDto>> GetGenealogy(
        int id,
        CancellationToken cancellationToken
    )
    {
        var genealogy = await mediator.Send(new GetAnimalGenealogyQuery(id), cancellationToken);
        if (genealogy == null)
        {
            return NotFound();
        }

        return Ok(genealogy);
    }

    [HttpPost]
    public async Task<ActionResult<AnimalDto>> Create(
        CreateAnimalDto dto,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var animal = await mediator.Send(new CreateAnimalCommand(dto), cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = animal.Id }, animal);
        }
        catch (Exception ex)
        {
            return HandleServiceException(ex);
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<AnimalDto>> Update(
        int id,
        UpdateAnimalDto dto,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var animal = await mediator.Send(new UpdateAnimalCommand(id, dto), cancellationToken);
            return Ok(animal);
        }
        catch (Exception ex)
        {
            return HandleServiceException(ex);
        }
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        try
        {
            await mediator.Send(new DeleteAnimalCommand(id), cancellationToken);
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
        [FromBody] MoveAnimalRequest request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var animal = await mediator.Send(
                new MoveAnimalCommand(id, request.FromLotId, request.ToLotId, request.Reason),
                cancellationToken
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
        [FromForm] string? description,
        CancellationToken cancellationToken
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

            var result = await mediator.Send(command, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return HandleServiceException(ex);
        }
    }

    [HttpPut("{id}/photos/{photoId}/profile")]
    public async Task<ActionResult> SetProfilePhoto(
        int id,
        int photoId,
        CancellationToken cancellationToken
    )
    {
        try
        {
            await mediator.Send(new SetAnimalProfilePhotoCommand(id, photoId), cancellationToken);
            return NoContent();
        }
        catch (Exception ex)
        {
            return HandleServiceException(ex);
        }
    }

    [HttpDelete("{id}/photos/{photoId}")]
    public async Task<ActionResult> DeletePhoto(
        int id,
        int photoId,
        CancellationToken cancellationToken
    )
    {
        try
        {
            await mediator.Send(new DeleteAnimalPhotoCommand(id, photoId), cancellationToken);
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
