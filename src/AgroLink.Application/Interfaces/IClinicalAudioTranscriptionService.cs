using AgroLink.Application.Features.ClinicalCases.Models;

namespace AgroLink.Application.Interfaces;

public interface IClinicalAudioTranscriptionService
{
    Task<string?> TranscribeAsync(
        ClinicalAudioTranscriptionRequest request,
        CancellationToken ct = default
    );
}
