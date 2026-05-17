using AgroLink.Domain.Enums;

namespace AgroLink.Domain.Entities;

public class AnimalBcsReading
{
    public int Id { get; set; }

    public int AnimalId { get; set; }

    public double Score { get; set; }

    public BcsReadingSource Source { get; set; }

    public int ConfirmedByUserId { get; set; }

    public bool HasAlerts { get; set; }

    public string? AlertDescription { get; set; }

    public string? RawAiResponse { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual Animal Animal { get; set; } = null!;
    public virtual User ConfirmedByUser { get; set; } = null!;
}
