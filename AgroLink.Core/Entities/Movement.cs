using System.ComponentModel.DataAnnotations;

namespace AgroLink.Core.Entities;

public class Movement
{
    public int Id { get; set; }

    [Required]
    [MaxLength(20)]
    public string EntityType { get; set; } = string.Empty; // LOT, ANIMAL

    public int EntityId { get; set; }
    public int? FromId { get; set; } // Previous location
    public int? ToId { get; set; } // New location

    public DateTime At { get; set; } = DateTime.UtcNow;

    [MaxLength(500)]
    public string? Reason { get; set; }

    public int UserId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual User User { get; set; } = null!;
}
