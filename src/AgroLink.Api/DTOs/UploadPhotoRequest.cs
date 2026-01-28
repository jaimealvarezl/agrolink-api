namespace AgroLink.Api.DTOs;

public class UploadPhotoRequest
{
    public required string EntityType { get; set; }
    public required int EntityId { get; set; }
    public string? Description { get; set; }
    public IFormFile? File { get; set; }
}
