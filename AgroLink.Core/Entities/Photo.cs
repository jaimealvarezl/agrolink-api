using System.ComponentModel.DataAnnotations;

namespace AgroLink.Core.Entities;

public class Photo
{
    public int Id { get; set; }

    [Required]
    [MaxLength(20)]
    public string EntityType { get; set; } = string.Empty; // ANIMAL, CHECKLIST

    public int EntityId { get; set; }

    [Required]
    [MaxLength(500)]
    public string UriLocal { get; set; } = string.Empty; // Local file path

    [MaxLength(500)]
    public string? UriRemote { get; set; } // AWS S3 URL

    public bool Uploaded { get; set; }

    [MaxLength(200)]
    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
