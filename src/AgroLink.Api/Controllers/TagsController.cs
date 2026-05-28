using AgroLink.Api.DTOs.Tags;
using AgroLink.Application.Features.Tags.Commands.DeleteTag;
using AgroLink.Application.Features.Tags.Commands.RenameTag;
using AgroLink.Application.Features.Tags.Commands.UpdateTagColor;
using AgroLink.Application.Features.Tags.DTOs;
using AgroLink.Application.Features.Tags.Queries.GetFarmTags;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgroLink.Api.Controllers;

[Route("api/farms/{farmId}/tags")]
[Authorize(Policy = "FarmViewerAccess")]
public class TagsController(IMediator mediator) : BaseController
{
    [HttpGet("")]
    public async Task<ActionResult<List<TagDto>>> GetFarmTags(
        int farmId,
        [FromQuery] string? search,
        CancellationToken cancellationToken
    )
    {
        var tags = await mediator.Send(new GetFarmTagsQuery(farmId, search), cancellationToken);
        return Ok(tags);
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "FarmAdminAccess")]
    public async Task<ActionResult<TagDto>> RenameTag(
        int farmId,
        int id,
        [FromBody] RenameTagRequest request,
        CancellationToken cancellationToken
    )
    {
        var tag = await mediator.Send(
            new RenameTagCommand(id, farmId, request.DisplayName, GetCurrentUserId()),
            cancellationToken
        );
        return Ok(tag);
    }

    [HttpPatch("{id}/color")]
    [Authorize(Policy = "FarmAdminAccess")]
    public async Task<ActionResult<TagDto>> UpdateTagColor(
        int farmId,
        int id,
        [FromBody] UpdateTagColorRequest request,
        CancellationToken cancellationToken
    )
    {
        var tag = await mediator.Send(
            new UpdateTagColorCommand(id, farmId, request.ColorToken, GetCurrentUserId()),
            cancellationToken
        );
        return Ok(tag);
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "FarmAdminAccess")]
    public async Task<ActionResult<object>> DeleteTag(
        int farmId,
        int id,
        CancellationToken cancellationToken
    )
    {
        var affectedAnimals = await mediator.Send(
            new DeleteTagCommand(id, farmId, GetCurrentUserId()),
            cancellationToken
        );
        return Ok(new { affectedAnimals });
    }
}
