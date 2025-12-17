using AgroLink.Api.DTOs;
using AgroLink.Application.Features.Photos.Commands.DeletePhoto;
using AgroLink.Application.Features.Photos.Commands.SyncPendingPhotos;
using AgroLink.Application.Features.Photos.Commands.UploadPhoto;
using AgroLink.Application.Features.Photos.DTOs;
using AgroLink.Application.Features.Photos.Queries.GetPhotosByEntity;
using MediatR;
using Microsoft.AspNetCore.Mvc;

// Added this using directive

namespace AgroLink.Api.Controllers;

[Route("api/[controller]")]
public class PhotosController(IMediator mediator) : BaseController
{
    [HttpGet("entity/{entityType}/{entityId}")]
    public async Task<ActionResult<IEnumerable<PhotoDto>>> GetByEntity(
        string entityType,
        int entityId
    )
    {
        var photos = await mediator.Send(new GetPhotosByEntityQuery(entityType, entityId));
        return Ok(photos);
    }

    [HttpPost("upload")]
    public async Task<ActionResult<PhotoDto>> UploadPhoto([FromForm] UploadPhotoRequest request)
    {
        if (request.File == null || request.File.Length == 0)
        {
            return BadRequest("No file provided");
        }

        try
        {
            var dto = new CreatePhotoDto
            {
                EntityType = request.EntityType,
                EntityId = request.EntityId,
                Description = request.Description,
            };

            await using var stream = request.File.OpenReadStream();
            var photo = await mediator.Send(
                new UploadPhotoCommand(dto, stream, request.File.FileName)
            );
            return Ok(photo);
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
            await mediator.Send(new DeletePhotoCommand(id));
            return NoContent();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("sync")]
    public async Task<ActionResult> SyncPendingPhotos()
    {
        await mediator.Send(new SyncPendingPhotosCommand());
        return Ok(new { message = "Photo sync completed" });
    }
}
