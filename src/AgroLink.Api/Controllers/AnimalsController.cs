using AgroLink.Application.Common.Utilities;
using AgroLink.Application.Features.Animals.Commands.Create;
using AgroLink.Application.Features.Animals.Commands.CreateNote;
using AgroLink.Application.Features.Animals.Commands.Delete;
using AgroLink.Application.Features.Animals.Commands.DeleteNote;
using AgroLink.Application.Features.Animals.Commands.DeletePhoto;
using AgroLink.Application.Features.Animals.Commands.Move;
using AgroLink.Application.Features.Animals.Commands.Retire;
using AgroLink.Application.Features.Animals.Commands.SetProfilePhoto;
using AgroLink.Application.Features.Animals.Commands.Update;
using AgroLink.Application.Features.Animals.Commands.UploadPhoto;
using AgroLink.Application.Features.Animals.DTOs;
using AgroLink.Application.Features.Animals.Queries.GetAll;
using AgroLink.Application.Features.Animals.Queries.GetBreeds;
using AgroLink.Application.Features.Animals.Queries.GetById;
using AgroLink.Application.Features.Animals.Queries.GetByLot;
using AgroLink.Application.Features.Animals.Queries.GetColors;
using AgroLink.Application.Features.Animals.Queries.GetDetail;
using AgroLink.Application.Features.Animals.Queries.GetGenealogy;
using AgroLink.Application.Features.Animals.Queries.GetNotes;
using AgroLink.Application.Features.Animals.Queries.GetPagedList;
using AgroLink.Application.Features.Animals.Queries.GetTimeline;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgroLink.Api.Controllers;

[Route("api/farms/{farmId}/animals")]
[Authorize(Policy = "FarmViewerAccess")]
public class AnimalsController(IMediator mediator) : BaseController
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<AnimalDto>>> GetAll(
        int farmId,
        CancellationToken cancellationToken
    )
    {
        var animals = await mediator.Send(new GetAllAnimalsQuery(farmId), cancellationToken);
        return Ok(animals);
    }

    [HttpGet("colors")]
    public async Task<ActionResult<IEnumerable<string>>> GetColors(
        int farmId,
        CancellationToken cancellationToken
    )
    {
        var colors = await mediator.Send(new GetAnimalColorsQuery(farmId), cancellationToken);
        return Ok(colors);
    }

    [HttpGet("breeds")]
    public async Task<ActionResult<IEnumerable<string>>> GetBreeds(
        int farmId,
        CancellationToken cancellationToken
    )
    {
        var breeds = await mediator.Send(new GetAnimalBreedsQuery(farmId), cancellationToken);
        return Ok(breeds);
    }

    [HttpGet("search")]
    public async Task<ActionResult<PagedResult<AnimalListDto>>> GetPagedList(
        int farmId,
        [FromQuery] GetAnimalsPagedListQuery query,
        CancellationToken cancellationToken
    )
    {
        var queryWithFarmId = query with { FarmId = farmId };
        var result = await mediator.Send(queryWithFarmId, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<AnimalDto>> GetById(
        int farmId,
        int id,
        CancellationToken cancellationToken
    )
    {
        var animal = await mediator.Send(
            new GetAnimalByIdQuery(id, GetCurrentUserId()),
            cancellationToken
        );
        if (animal == null)
        {
            return NotFound();
        }

        return Ok(animal);
    }

    [HttpGet("{id}/details")]
    public async Task<ActionResult<AnimalDetailDto>> GetDetail(
        int farmId,
        int id,
        CancellationToken cancellationToken
    )
    {
        var animal = await mediator.Send(
            new GetAnimalDetailQuery(id, GetCurrentUserId()),
            cancellationToken
        );
        if (animal == null)
        {
            return NotFound();
        }

        return Ok(animal);
    }

    [HttpGet("lot/{lotId}")]
    public async Task<ActionResult<IEnumerable<AnimalDto>>> GetByLot(
        int farmId,
        int lotId,
        CancellationToken cancellationToken
    )
    {
        var animals = await mediator.Send(
            new GetAnimalsByLotQuery(lotId, GetCurrentUserId()),
            cancellationToken
        );
        return Ok(animals);
    }

    [HttpGet("{id}/genealogy")]
    public async Task<ActionResult<AnimalGenealogyDto>> GetGenealogy(
        int farmId,
        int id,
        CancellationToken cancellationToken
    )
    {
        var genealogy = await mediator.Send(
            new GetAnimalGenealogyQuery(id, GetCurrentUserId()),
            cancellationToken
        );
        if (genealogy == null)
        {
            return NotFound();
        }

        return Ok(genealogy);
    }

    [HttpPost]
    [Authorize(Policy = "FarmEditorAccess")]
    public async Task<ActionResult<AnimalDto>> Create(
        int farmId,
        CreateAnimalDto dto,
        CancellationToken cancellationToken
    )
    {
        var animal = await mediator.Send(new CreateAnimalCommand(dto), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { farmId, id = animal.Id }, animal);
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "FarmEditorAccess")]
    public async Task<ActionResult<AnimalDto>> Update(
        int farmId,
        int id,
        UpdateAnimalDto dto,
        CancellationToken cancellationToken
    )
    {
        var animal = await mediator.Send(
            new UpdateAnimalCommand(id, dto, GetCurrentUserId()),
            cancellationToken
        );
        return Ok(animal);
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "FarmAdminAccess")]
    public async Task<ActionResult> Delete(int farmId, int id, CancellationToken cancellationToken)
    {
        await mediator.Send(new DeleteAnimalCommand(id, GetCurrentUserId()), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id}/move")]
    [Authorize(Policy = "FarmEditorAccess")]
    public async Task<ActionResult<AnimalDto>> MoveAnimal(
        int farmId,
        int id,
        [FromBody] MoveAnimalRequest request,
        CancellationToken cancellationToken
    )
    {
        var animal = await mediator.Send(
            new MoveAnimalCommand(
                id,
                request.FromLotId,
                request.ToLotId,
                GetCurrentUserId(),
                request.Reason
            ),
            cancellationToken
        );
        return Ok(animal);
    }

    [HttpPost("{id}/photos")]
    [Consumes("multipart/form-data")]
    [Authorize(Policy = "FarmEditorAccess")]
    public async Task<ActionResult<AnimalPhotoDto>> UploadPhoto(
        int farmId,
        int id,
        IFormFile file,
        [FromForm] string? description,
        CancellationToken cancellationToken
    )
    {
        await using var stream = file.OpenReadStream();
        var command = new UploadAnimalPhotoCommand(
            id,
            stream,
            file.FileName,
            file.ContentType,
            file.Length,
            GetCurrentUserId(),
            description
        );

        var result = await mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    [HttpPut("{id}/photos/{photoId}/profile")]
    [Authorize(Policy = "FarmEditorAccess")]
    public async Task<ActionResult> SetProfilePhoto(
        int farmId,
        int id,
        int photoId,
        CancellationToken cancellationToken
    )
    {
        await mediator.Send(
            new SetAnimalProfilePhotoCommand(id, photoId, GetCurrentUserId()),
            cancellationToken
        );
        return NoContent();
    }

    [HttpDelete("{id}/photos/{photoId}")]
    [Authorize(Policy = "FarmEditorAccess")]
    public async Task<ActionResult> DeletePhoto(
        int farmId,
        int id,
        int photoId,
        CancellationToken cancellationToken
    )
    {
        await mediator.Send(
            new DeleteAnimalPhotoCommand(id, photoId, GetCurrentUserId()),
            cancellationToken
        );
        return NoContent();
    }

    [HttpGet("{id}/notes")]
    public async Task<ActionResult<IEnumerable<AnimalNoteDto>>> GetNotes(
        int farmId,
        int id,
        CancellationToken cancellationToken
    )
    {
        var notes = await mediator.Send(new GetAnimalNotesQuery(id, farmId), cancellationToken);
        return Ok(notes);
    }

    [HttpPost("{id}/notes")]
    [Authorize(Policy = "FarmEditorAccess")]
    public async Task<ActionResult<AnimalNoteDto>> CreateNote(
        int farmId,
        int id,
        [FromBody] CreateAnimalNoteDto dto,
        CancellationToken cancellationToken
    )
    {
        var note = await mediator.Send(
            new CreateAnimalNoteCommand(farmId, id, dto.Content, GetCurrentUserId()),
            cancellationToken
        );
        return CreatedAtAction(nameof(GetNotes), new { farmId, id }, note);
    }

    [HttpDelete("{id}/notes/{noteId}")]
    [Authorize(Policy = "FarmEditorAccess")]
    public async Task<ActionResult> DeleteNote(
        int farmId,
        int id,
        int noteId,
        CancellationToken cancellationToken
    )
    {
        await mediator.Send(
            new DeleteAnimalNoteCommand(id, noteId, GetCurrentUserId()),
            cancellationToken
        );
        return NoContent();
    }

    [HttpPost("{id}/retire")]
    [Authorize(Policy = "FarmEditorAccess")]
    public async Task<ActionResult<AnimalRetirementDto>> Retire(
        int farmId,
        int id,
        [FromBody] RetireAnimalRequest request,
        CancellationToken cancellationToken
    )
    {
        var result = await mediator.Send(
            new RetireAnimalCommand(
                farmId,
                id,
                GetCurrentUserId(),
                request.Reason,
                request.At,
                request.SalePrice,
                request.Notes
            ),
            cancellationToken
        );
        return Ok(result);
    }

    [HttpGet("{id}/timeline")]
    public async Task<ActionResult<IEnumerable<AnimalTimelineItemDto>>> GetTimeline(
        int farmId,
        int id,
        CancellationToken cancellationToken
    )
    {
        var timeline = await mediator.Send(
            new GetAnimalTimelineQuery(id, farmId),
            cancellationToken
        );
        return Ok(timeline);
    }
}
