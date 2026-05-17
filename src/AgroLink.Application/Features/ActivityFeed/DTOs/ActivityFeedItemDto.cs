namespace AgroLink.Application.Features.ActivityFeed.DTOs;

public record ActivityFeedItemDto
{
    public ActivityFeedEventType EventType { get; init; }
    public int AnimalId { get; init; }
    public string? AnimalName { get; init; }
    public DateTime OccurredAt { get; init; }

    // Structured context — one field populated per event type; frontend builds the display string
    public string? ToLotName { get; init; }
    public string? NoteContent { get; init; }
    public string? RetirementReason { get; init; }
}
