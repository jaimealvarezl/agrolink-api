namespace AgroLink.Application.Features.ClinicalCases.Commands.ReceiveTelegramUpdate;

public class ReceiveTelegramUpdateResult
{
    public bool Processed { get; init; }
    public string Status { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
}
