using System.ComponentModel.DataAnnotations;

namespace AgroLink.Domain.Entities;

public class Paddock
{
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    public decimal? Area { get; set; }

    [MaxLength(50)]
    public string? AreaType { get; set; }

    [Required]
    public int FarmId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public virtual Farm Farm { get; set; } = null!;
    public virtual ICollection<Lot> Lots { get; set; } = new List<Lot>();
}
