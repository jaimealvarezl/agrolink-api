namespace AgroLink.Application.Features.ClinicalCases.Models;

public class TelegramSendResult
{
    public bool Success { get; init; }
    public long? TelegramMessageId { get; init; }
    public string ProviderResponse { get; init; } = string.Empty;
}
