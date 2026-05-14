using AgroLink.Application.Common.Exceptions;
using AgroLink.Application.Features.Animals.DTOs;
using AgroLink.Application.Features.Animals.Models;
using AgroLink.Application.Interfaces;
using AgroLink.Domain.Interfaces;
using MediatR;

namespace AgroLink.Application.Features.Animals.Queries.AnalyzeHealth;

public class AnalyzeAnimalHealthQueryHandler(
    IAnimalRepository animalRepository,
    IAnimalHealthAnalysisService healthAnalysisService
) : IRequestHandler<AnalyzeAnimalHealthQuery, AnimalHealthAnalysisDto>
{
    public async Task<AnimalHealthAnalysisDto> Handle(
        AnalyzeAnimalHealthQuery request,
        CancellationToken cancellationToken
    )
    {
        var animal = await animalRepository.GetAnimalDetailsAsync(
            request.AnimalId,
            request.UserId,
            cancellationToken
        );

        if (animal == null || animal.Lot?.Paddock?.FarmId != request.FarmId)
        {
            throw new NotFoundException("Animal", request.AnimalId);
        }

        var primaryPhoto = animal
            .Photos.OrderByDescending(p => p.IsProfile)
            .ThenByDescending(p => p.UploadedAt)
            .FirstOrDefault();

        if (primaryPhoto == null)
        {
            throw new ArgumentException("El animal no tiene fotos para analizar.");
        }

        var analysisRequest = new AnimalHealthAnalysisRequest
        {
            AnimalName = animal.Name,
            Breed = animal.Breed,
            Sex = animal.Sex,
            BirthDate = animal.BirthDate,
            ProductionStatus = animal.ProductionStatus,
            ReproductiveStatus = animal.ReproductiveStatus,
            PhotoStorageKey = primaryPhoto.StorageKey,
            PhotoContentType = primaryPhoto.ContentType,
        };

        var result = await healthAnalysisService.AnalyzeAsync(analysisRequest, cancellationToken);

        if (result.PhotoRejected)
        {
            throw new ArgumentException(
                "La imagen no es lo suficientemente clara para realizar un análisis."
            );
        }

        return new AnimalHealthAnalysisDto
        {
            EstimatedBcs = result.BodyConditionScore,
            HasAlerts = result.HasAlert,
            AlertDescription = result.AlertDescription,
        };
    }
}
