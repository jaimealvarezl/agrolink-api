namespace AgroLink.Application.Features.Notifications.DTOs;

public record BirthWatchScanSummaryDto(
    DateTime ScannedAt,
    DateOnly WindowEnd,
    int Candidates,
    int Sent,
    int Skipped,
    int PrunedTokens
);
