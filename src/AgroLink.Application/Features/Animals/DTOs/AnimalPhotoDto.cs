namespace AgroLink.Application.Features.Animals.DTOs;

public record AnimalPhotoDto
{
    public int Id { get; init; }
    public int AnimalId { get; init; }
    public string UriRemote { get; init; } = string.Empty;
    public bool IsProfile { get; init; }
    public string ContentType { get; init; } = string.Empty;
    public long Size { get; init; }
    public string? Description { get; init; }
    public DateTime UploadedAt { get; init; }
    public DateTime CreatedAt { get; init; }
}
