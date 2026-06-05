using AgroLink.Domain.Enums;

namespace AgroLink.Domain.Entities;

public class ReproductiveEvent
{
    public int Id { get; set; }

    public int AnimalId { get; set; }

    public ReproductiveEventType EventType { get; set; }

    public DateTime Date { get; set; }

    public int? BullId { get; set; }

    public ReproductiveEventStatus Status { get; set; } = ReproductiveEventStatus.Positive;

    public int? EstimatedMonths { get; set; }

    public DateTime? ExpectedDueDate { get; set; }

    public int CreatedByUserId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public virtual Animal Animal { get; set; } = null!;
    public virtual Animal? Bull { get; set; }
    public virtual User CreatedByUser { get; set; } = null!;
}
