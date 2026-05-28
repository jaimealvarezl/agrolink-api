using System.ComponentModel.DataAnnotations;

namespace AgroLink.Domain.Entities;

public class Tag
{
    public int Id { get; set; }

    public int FarmId { get; set; }

    [Required]
    [MaxLength(24)]
    public string CanonicalName { get; set; } = string.Empty;

    [Required]
    [MaxLength(24)]
    public string DisplayName { get; set; } = string.Empty;

    [MaxLength(32)]
    public string? ColorToken { get; set; }

    public int CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public virtual Farm Farm { get; set; } = null!;
    public virtual ICollection<AnimalTag> AnimalTags { get; set; } = new List<AnimalTag>();
}
