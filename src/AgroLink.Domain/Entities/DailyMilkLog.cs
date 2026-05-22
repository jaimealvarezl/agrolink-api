using System.ComponentModel.DataAnnotations;

namespace AgroLink.Domain.Entities;

public class DailyMilkLog
{
    public int Id { get; set; }
    public int FarmId { get; set; }
    public DateOnly Date { get; set; }
    public decimal TotalLiters { get; set; }
    public decimal? PricePerLiter { get; set; }
    public int UserId { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public virtual Farm Farm { get; set; } = null!;
    public virtual User User { get; set; } = null!;
}
