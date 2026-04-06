namespace AgroLink.Application.Features.ExternalWorkers.Models;

public record SendTelegramVoicePayload(
    long ChatId,
    string Base64AudioContent,
    string FileName,
    string MimeType,
    string? Caption
);
