using System.ComponentModel.DataAnnotations;

namespace AgroLink.Domain.Entities;

public class Movement
{
    public int Id { get; set; }

    public int AnimalId { get; set; }

    public int? FromLotId { get; set; } // Previous lot location
    public int? ToLotId { get; set; } // New lot location

    public DateTime At { get; set; } = DateTime.UtcNow;

    [MaxLength(500)]
    public string? Reason { get; set; }

    public int UserId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual Animal Animal { get; set; } = null!;
    public virtual Lot? FromLot { get; set; }
    public virtual Lot? ToLot { get; set; }
    public virtual User User { get; set; } = null!;
}
