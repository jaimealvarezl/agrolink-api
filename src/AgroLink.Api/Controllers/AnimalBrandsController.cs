using AgroLink.Api.DTOs.AnimalBrands;
using AgroLink.Application.Features.AnimalBrands.Commands.Add;
using AgroLink.Application.Features.AnimalBrands.Commands.Remove;
using AgroLink.Application.Features.AnimalBrands.DTOs;
using AgroLink.Application.Features.AnimalBrands.Queries.GetByAnimal;
using AgroLink.Application.Features.AnimalBrands.Queries.GetSuggestions;
using AgroLink.Application.Features.OwnerBrands.DTOs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgroLink.Api.Controllers;

[Route("api/farms/{farmId}/animals/{animalId}")]
public class AnimalBrandsController(IMediator mediator) : BaseController
{
    [HttpGet("brands")]
    [Authorize(Policy = "FarmViewerAccess")]
    public async Task<ActionResult<IEnumerable<AnimalBrandDto>>> GetBrands(
        int farmId,
        int animalId,
        CancellationToken cancellationToken
    )
    {
        var brands = await mediator.Send(
            new GetAnimalBrandsQuery(farmId, animalId),
            cancellationToken
        );
        return Ok(brands);
    }

    [HttpPost("brands")]
    [Authorize(Policy = "FarmEditorAccess")]
    public async Task<ActionResult<AnimalBrandDto>> AddBrand(
        int farmId,
        int animalId,
        AddAnimalBrandRequest request,
        CancellationToken cancellationToken
    )
    {
        var brand = await mediator.Send(
            new AddAnimalBrandCommand(
                farmId,
                animalId,
                request.OwnerBrandId,
                request.AppliedAt,
                request.Notes
            ),
            cancellationToken
        );
        return CreatedAtAction(nameof(GetBrands), new { farmId, animalId }, brand);
    }

    [HttpDelete("brands/{animalBrandId}")]
    [Authorize(Policy = "FarmEditorAccess")]
    public async Task<ActionResult> RemoveBrand(
        int farmId,
        int animalId,
        int animalBrandId,
        CancellationToken cancellationToken
    )
    {
        await mediator.Send(
            new RemoveAnimalBrandCommand(farmId, animalId, animalBrandId),
            cancellationToken
        );
        return NoContent();
    }

    [HttpGet("brand-suggestions")]
    [Authorize(Policy = "FarmViewerAccess")]
    public async Task<ActionResult<IEnumerable<OwnerBrandDto>>> GetBrandSuggestions(
        int farmId,
        int animalId,
        CancellationToken cancellationToken
    )
    {
        var suggestions = await mediator.Send(
            new GetAnimalBrandSuggestionsQuery(farmId, animalId),
            cancellationToken
        );
        return Ok(suggestions);
    }
}
