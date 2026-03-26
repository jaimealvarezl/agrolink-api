using System.ComponentModel.DataAnnotations;
using AgroLink.Domain.Enums;

namespace AgroLink.Domain.Entities;

public class AnimalRetirement
{
    public int Id { get; set; }

    public int AnimalId { get; set; }

    public int UserId { get; set; }

    public RetirementReason Reason { get; set; }

    public DateTime At { get; set; }

    public decimal? SalePrice { get; set; }

    [MaxLength(2000)]
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public virtual Animal Animal { get; set; } = null!;
    public virtual User User { get; set; } = null!;
}
