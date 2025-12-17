namespace AgroLink.Api.DTOs;

public class UploadPhotoRequest
{
    public string EntityType { get; set; } = string.Empty;
    public int EntityId { get; set; }
    public string? Description { get; set; }
    public IFormFile? File { get; set; }
}
