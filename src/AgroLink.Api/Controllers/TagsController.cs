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

[Route("api")]
public class TagsController(IMediator mediator) : BaseController
{
    [HttpGet("farms/{farmId}/tags")]
    [Authorize(Policy = "FarmViewerAccess")]
    public async Task<ActionResult<List<TagDto>>> GetFarmTags(
        int farmId,
        [FromQuery] string? search,
        CancellationToken cancellationToken
    )
    {
        var tags = await mediator.Send(new GetFarmTagsQuery(farmId, search), cancellationToken);
        return Ok(tags);
    }

    [HttpPut("tags/{id}")]
    [Authorize]
    public async Task<ActionResult<TagDto>> RenameTag(
        int id,
        [FromBody] RenameTagRequest request,
        CancellationToken cancellationToken
    )
    {
        var tag = await mediator.Send(
            new RenameTagCommand(id, request.DisplayName, GetCurrentUserId()),
            cancellationToken
        );
        return Ok(tag);
    }

    [HttpPatch("tags/{id}/color")]
    [Authorize]
    public async Task<ActionResult<TagDto>> UpdateTagColor(
        int id,
        [FromBody] UpdateTagColorRequest request,
        CancellationToken cancellationToken
    )
    {
        var tag = await mediator.Send(
            new UpdateTagColorCommand(id, request.ColorToken, GetCurrentUserId()),
            cancellationToken
        );
        return Ok(tag);
    }

    [HttpDelete("tags/{id}")]
    [Authorize]
    public async Task<ActionResult<object>> DeleteTag(int id, CancellationToken cancellationToken)
    {
        var affectedAnimals = await mediator.Send(
            new DeleteTagCommand(id, GetCurrentUserId()),
            cancellationToken
        );
        return Ok(new { affectedAnimals });
    }
}
