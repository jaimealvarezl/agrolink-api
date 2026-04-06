namespace AgroLink.Application.Features.ExternalWorkers.Models;

public record TranscribeAudioPayload(string Base64AudioContent, string FileName, string? MimeType);
