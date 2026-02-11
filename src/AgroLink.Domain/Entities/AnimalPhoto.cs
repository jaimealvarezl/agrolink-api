using System.ComponentModel.DataAnnotations;

namespace AgroLink.Domain.Entities;

public class AnimalPhoto
{
    public int Id { get; set; }

    public int AnimalId { get; set; }

    [Required]
    [MaxLength(500)]
    public string UriRemote { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string StorageKey { get; set; } = string.Empty;

    public bool IsProfile { get; set; }

    [Required]
    [MaxLength(100)]
    public string ContentType { get; set; } = string.Empty;

    public long Size { get; set; }

    [MaxLength(200)]
    public string? Description { get; set; }

    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual Animal Animal { get; set; } = null!;
}
