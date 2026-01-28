namespace AgroLink.Application.Features.Photos.DTOs;

public class PhotoDto
{
    public required int Id { get; set; }
    public required string EntityType { get; set; }
    public required int EntityId { get; set; }
    public required string UriLocal { get; set; }
    public string? UriRemote { get; set; }
    public required bool Uploaded { get; set; }
    public string? Description { get; set; }
    public required DateTime CreatedAt { get; set; }
}

public class CreatePhotoDto
{
    public string EntityType { get; set; } = string.Empty;
    public int EntityId { get; set; }
    public string? Description { get; set; }
}
