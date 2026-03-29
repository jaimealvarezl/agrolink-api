using AgroLink.Api.DTOs.OwnerBrands;
using AgroLink.Application.Features.OwnerBrands.Commands.Create;
using AgroLink.Application.Features.OwnerBrands.Commands.Delete;
using AgroLink.Application.Features.OwnerBrands.Commands.Update;
using AgroLink.Application.Features.OwnerBrands.DTOs;
using AgroLink.Application.Features.OwnerBrands.Queries.GetByOwner;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgroLink.Api.Controllers;

[Route("api/farms/{farmId}/owners/{ownerId}/brands")]
public class OwnerBrandsController(IMediator mediator) : BaseController
{
    [HttpGet]
    [Authorize(Policy = "FarmEditorAccess")]
    public async Task<ActionResult<IEnumerable<OwnerBrandDto>>> GetBrands(
        int farmId,
        int ownerId,
        CancellationToken cancellationToken
    )
    {
        var brands = await mediator.Send(new GetOwnerBrandsQuery(farmId, ownerId), cancellationToken);
        return Ok(brands);
    }

    [HttpPost]
    [Authorize(Policy = "FarmAdminAccess")]
    public async Task<ActionResult<OwnerBrandDto>> Create(
        int farmId,
        int ownerId,
        CreateOwnerBrandRequest request
    )
    {
        var brand = await mediator.Send(
            new CreateOwnerBrandCommand(
                farmId,
                ownerId,
                request.RegistrationNumber,
                request.Description,
                request.PhotoUrl
            )
        );
        return CreatedAtAction(nameof(GetBrands), new { farmId, ownerId }, brand);
    }

    [HttpPut("{brandId}")]
    [Authorize(Policy = "FarmAdminAccess")]
    public async Task<ActionResult<OwnerBrandDto>> Update(
        int farmId,
        int ownerId,
        int brandId,
        UpdateOwnerBrandRequest request
    )
    {
        var brand = await mediator.Send(
            new UpdateOwnerBrandCommand(
                farmId,
                ownerId,
                brandId,
                request.RegistrationNumber,
                request.Description,
                request.PhotoUrl
            )
        );
        return Ok(brand);
    }

    [HttpDelete("{brandId}")]
    [Authorize(Policy = "FarmAdminAccess")]
    public async Task<ActionResult> Delete(int farmId, int ownerId, int brandId)
    {
        await mediator.Send(new DeleteOwnerBrandCommand(farmId, ownerId, brandId));
        return NoContent();
    }
}
