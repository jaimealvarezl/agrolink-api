using AgroLink.Application.Features.Animals.Models;

namespace AgroLink.Application.Interfaces;

public interface IAnimalHealthAnalysisService
{
    Task<AnimalHealthAnalysisResult> AnalyzeAsync(
        AnimalHealthAnalysisRequest request,
        CancellationToken ct = default
    );
}
