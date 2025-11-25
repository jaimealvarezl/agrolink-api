namespace AgroLink.Core.DTOs;

public class PhotoDto
{
    public int Id { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public int EntityId { get; set; }
    public string UriLocal { get; set; } = string.Empty;
    public string? UriRemote { get; set; }
    public bool Uploaded { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreatePhotoDto
{
    public string EntityType { get; set; } = string.Empty;
    public int EntityId { get; set; }
    public string? Description { get; set; }
}
