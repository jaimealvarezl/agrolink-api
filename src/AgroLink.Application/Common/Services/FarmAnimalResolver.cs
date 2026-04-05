using AgroLink.Application.Features.ClinicalCases.Models;
using AgroLink.Application.Interfaces;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;

namespace AgroLink.Application.Common.Services;

public class FarmAnimalResolver(IFarmRepository farmRepository, IAnimalRepository animalRepository)
    : IFarmAnimalResolver
{
    public async Task<FarmAnimalResolutionResult> ResolveAsync(
        string? farmReference,
        string? animalReference,
        string? earTag,
        CancellationToken ct = default
    )
    {
        if (string.IsNullOrWhiteSpace(farmReference))
        {
            return new FarmAnimalResolutionResult
            {
                ResolutionMessage =
                    "No pude identificar la granja. Indica algo como: granja: NombreGranja",
            };
        }

        var farm = await farmRepository.FindByReferenceAsync(farmReference, ct);
        if (farm == null)
        {
            return new FarmAnimalResolutionResult
            {
                ResolutionMessage =
                    $"No encontre la granja '{farmReference}'. Revisa el nombre o CUE en AgroLink.",
            };
        }

        var animal = default(Animal);

        if (!string.IsNullOrWhiteSpace(earTag))
        {
            animal = await animalRepository.GetByEarTagInFarmAsync(farm.Id, earTag, ct);
        }

        if (animal == null && !string.IsNullOrWhiteSpace(animalReference))
        {
            animal = await animalRepository.FindByReferenceInFarmAsync(
                farm.Id,
                animalReference,
                ct
            );
        }

        if (animal == null)
        {
            return new FarmAnimalResolutionResult
            {
                Farm = farm,
                ResolutionMessage =
                    "No pude identificar el animal en la granja indicada. "
                    + "Comparte arete/tag o nombre exacto del animal.",
            };
        }

        return new FarmAnimalResolutionResult
        {
            Farm = farm,
            Animal = animal,
            ResolutionMessage = "ok",
        };
    }
}
