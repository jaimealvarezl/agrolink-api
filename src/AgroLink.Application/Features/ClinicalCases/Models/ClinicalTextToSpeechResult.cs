namespace AgroLink.Application.Features.ClinicalCases.Models;

public class ClinicalTextToSpeechResult
{
    public bool Success { get; init; }
    public byte[] AudioContent { get; init; } = [];
    public string FileName { get; init; } = string.Empty;
    public string MimeType { get; init; } = "audio/mpeg";
    public string ProviderResponse { get; init; } = string.Empty;
}
