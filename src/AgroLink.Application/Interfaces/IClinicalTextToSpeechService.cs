using AgroLink.Application.Features.ClinicalCases.Models;

namespace AgroLink.Application.Interfaces;

public interface IClinicalTextToSpeechService
{
    Task<ClinicalTextToSpeechResult?> SynthesizeAsync(
        ClinicalTextToSpeechRequest request,
        CancellationToken ct = default
    );
}
