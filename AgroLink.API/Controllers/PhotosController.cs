using AgroLink.Core.DTOs;
using AgroLink.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AgroLink.API.Controllers;

[Route("api/[controller]")]
public class PhotosController(IPhotoService photoService) : BaseController
{
    [HttpGet("entity/{entityType}/{entityId}")]
    public async Task<ActionResult<IEnumerable<PhotoDto>>> GetByEntity(
        string entityType,
        int entityId
    )
    {
        var photos = await photoService.GetByEntityAsync(entityType, entityId);
        return Ok(photos);
    }

    [HttpPost("upload")]
    public async Task<ActionResult<PhotoDto>> UploadPhoto(
        [FromForm] CreatePhotoDto dto,
        [FromForm] IFormFile? file
    )
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("No file provided");
        }

        try
        {
            await using var stream = file.OpenReadStream();
            var photo = await photoService.UploadPhotoAsync(dto, stream, file.FileName);
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
            await photoService.DeleteAsync(id);
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
        await photoService.SyncPendingPhotosAsync();
        return Ok(new { message = "Photo sync completed" });
    }
}
