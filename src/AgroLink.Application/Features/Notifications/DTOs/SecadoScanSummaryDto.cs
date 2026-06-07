namespace AgroLink.Application.Features.Notifications.DTOs;

public record SecadoScanSummaryDto(
    DateTime ScannedAt,
    DateOnly TargetDate,
    int Candidates,
    int Sent,
    int Skipped,
    int PrunedTokens
);
