using AgroLink.Domain.Enums;

namespace AgroLink.Application.Features.ReproductiveEvents.DTOs;

public record ReproductiveEventDto
{
    public int Id { get; init; }
    public int AnimalId { get; init; }
    public ReproductiveEventType EventType { get; init; }
    public DateTime Date { get; init; }
    public int? BullId { get; init; }
    public ReproductiveEventStatus Status { get; init; }
    public int? EstimatedMonths { get; init; }
    public DateTime? ExpectedDueDate { get; init; }
    public int CreatedByUserId { get; init; }
    public DateTime CreatedAt { get; init; }
}
