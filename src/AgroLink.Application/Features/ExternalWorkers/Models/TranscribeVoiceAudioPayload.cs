namespace AgroLink.Application.Features.ExternalWorkers.Models;

public record TranscribeVoiceAudioPayload(
    string Base64AudioContent,
    string FileName,
    string? MimeType,
    string Language = "es"
);
