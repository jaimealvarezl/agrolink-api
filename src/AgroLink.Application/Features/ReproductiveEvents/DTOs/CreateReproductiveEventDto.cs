using AgroLink.Domain.Enums;

namespace AgroLink.Application.Features.ReproductiveEvents.DTOs;

public record CreateReproductiveEventDto
{
    public ReproductiveEventType EventType { get; init; }
    public DateTime Date { get; init; }
    public int? BullId { get; init; }
    public ReproductiveEventStatus? Status { get; init; }
    public int? EstimatedMonths { get; init; }
}
