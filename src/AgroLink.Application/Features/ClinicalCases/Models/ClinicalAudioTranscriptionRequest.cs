namespace AgroLink.Application.Features.ClinicalCases.Models;

public record ClinicalAudioTranscriptionRequest(
    byte[] AudioContent,
    string FileName,
    string? MimeType
);
