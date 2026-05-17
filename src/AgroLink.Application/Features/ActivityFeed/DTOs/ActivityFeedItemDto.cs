namespace AgroLink.Application.Features.ActivityFeed.DTOs;

public record ActivityFeedItemDto
{
    public string EventType { get; init; } = string.Empty;
    public int AnimalId { get; init; }
    public string? AnimalName { get; init; }
    public string Description { get; init; } = string.Empty;
    public DateTime OccurredAt { get; init; }
}
