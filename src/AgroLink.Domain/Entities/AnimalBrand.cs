using System.ComponentModel.DataAnnotations;

namespace AgroLink.Domain.Entities;

public class AnimalBrand
{
    public int Id { get; set; }

    public int AnimalId { get; set; }
    public virtual Animal Animal { get; set; } = null!;

    public int OwnerBrandId { get; set; }
    public virtual OwnerBrand OwnerBrand { get; set; } = null!;

    public DateTime? AppliedAt { get; set; }

    [MaxLength(2000)]
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
