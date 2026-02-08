using System.ComponentModel.DataAnnotations;

namespace AgroLink.Application.Features.Photos.DTOs;

public class PhotoDto
{
    public required int Id { get; set; }

    [Required]
    public required string EntityType { get; set; }

    [Required]
    public required int EntityId { get; set; }

    [Required]
    public required string UriLocal { get; set; }

    public string? UriRemote { get; set; }

    [Required]
    public required bool Uploaded { get; set; }

    public string? Description { get; set; }

    [Required]
    public required DateTime CreatedAt { get; set; }
}

public class CreatePhotoDto
{
    [Required]
    public string EntityType { get; set; } = string.Empty;

    [Required]
    public int EntityId { get; set; }

    public string? Description { get; set; }
}
