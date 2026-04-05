using AgroLink.Application.Features.ClinicalCases.Models;

namespace AgroLink.Application.Interfaces;

public interface IFarmAnimalResolver
{
    Task<FarmAnimalResolutionResult> ResolveAsync(
        string? farmReference,
        string? animalReference,
        string? earTag,
        CancellationToken ct = default
    );
}
