using System.ComponentModel.DataAnnotations;

namespace AgroLink.Api.DTOs;

public class UploadPhotoRequest
{
    [Required]
    public required string EntityType { get; set; }

    [Required]
    public required int EntityId { get; set; }
    public string? Description { get; set; }

    [Required]
    public IFormFile? File { get; set; }
}
