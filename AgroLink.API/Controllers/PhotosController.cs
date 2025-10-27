using AgroLink.Core.DTOs;
using AgroLink.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgroLink.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PhotosController : ControllerBase
{
    private readonly IPhotoService _photoService;

    public PhotosController(IPhotoService photoService)
    {
        _photoService = photoService;
    }

    [HttpGet("entity/{entityType}/{entityId}")]
    public async Task<ActionResult<IEnumerable<PhotoDto>>> GetByEntity(
        string entityType,
        int entityId
    )
    {
        var photos = await _photoService.GetByEntityAsync(entityType, entityId);
        return Ok(photos);
    }

    [HttpPost("upload")]
    public async Task<ActionResult<PhotoDto>> UploadPhoto(
        [FromForm] CreatePhotoDto dto,
        [FromForm] IFormFile file
    )
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file provided");

        try
        {
            using var stream = file.OpenReadStream();
            var photo = await _photoService.UploadPhotoAsync(dto, stream, file.FileName);
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
            await _photoService.DeleteAsync(id);
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
        await _photoService.SyncPendingPhotosAsync();
        return Ok(new { message = "Photo sync completed" });
    }
}
