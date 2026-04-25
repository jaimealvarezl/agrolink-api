namespace AgroLink.Application.Interfaces;

public interface IVoiceTranscriptionService
{
    Task<string?> TranscribeAsync(
        byte[] audioContent,
        string fileName,
        string? mimeType,
        string language,
        CancellationToken ct = default
    );
}
