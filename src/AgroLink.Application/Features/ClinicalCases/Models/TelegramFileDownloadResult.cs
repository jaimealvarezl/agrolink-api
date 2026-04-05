namespace AgroLink.Application.Features.ClinicalCases.Models;

public class TelegramFileDownloadResult
{
    public bool Success { get; init; }
    public byte[] Content { get; init; } = [];
    public string FilePath { get; init; } = string.Empty;
    public string ContentType { get; init; } = "application/octet-stream";
    public string ProviderResponse { get; init; } = string.Empty;
}
