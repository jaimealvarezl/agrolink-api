using AgroLink.Application.Features.ReproductiveEvents.Commands.Create;
using AgroLink.Application.Features.ReproductiveEvents.DTOs;
using AgroLink.Application.Features.ReproductiveEvents.Queries.GetByAnimal;
using AgroLink.Application.Features.ReproductiveEvents.Queries.GetById;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgroLink.Api.Controllers;

[Route("api/farms/{farmId}/animals/{animalId}/reproductive-events")]
[Authorize(Policy = "FarmViewerAccess")]
public class ReproductiveEventsController(IMediator mediator) : BaseController
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ReproductiveEventDto>>> GetByAnimal(
        int farmId,
        int animalId,
        CancellationToken cancellationToken
    )
    {
        var result = await mediator.Send(
            new GetReproductiveEventsByAnimalQuery(animalId, farmId),
            cancellationToken
        );

        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ReproductiveEventDto>> GetById(
        int farmId,
        int animalId,
        int id,
        CancellationToken cancellationToken
    )
    {
        var result = await mediator.Send(
            new GetReproductiveEventByIdQuery(id, animalId, farmId),
            cancellationToken
        );

        if (result is null)
        {
            return NotFound();
        }

        return Ok(result);
    }

    [HttpPost]
    [Authorize(Policy = "FarmEditorAccess")]
    public async Task<ActionResult<ReproductiveEventDto>> Create(
        int farmId,
        int animalId,
        [FromBody] CreateReproductiveEventDto dto,
        CancellationToken cancellationToken
    )
    {
        var result = await mediator.Send(
            new CreateReproductiveEventCommand(farmId, animalId, GetCurrentUserId(), dto),
            cancellationToken
        );

        return CreatedAtAction(
            nameof(GetById),
            new
            {
                farmId,
                animalId,
                id = result.Id,
            },
            result
        );
    }
}
