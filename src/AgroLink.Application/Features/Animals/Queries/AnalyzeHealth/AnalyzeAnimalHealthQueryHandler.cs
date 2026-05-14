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

        var primaryPhoto = animal.Photos?.OrderByDescending(p => p.UploadedAt).FirstOrDefault();

        if (primaryPhoto == null)
        {
            throw new ArgumentException("The animal has no photos to analyze.");
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
                string.IsNullOrWhiteSpace(result.RejectionReason)
                    ? "The image is not clear enough to perform an analysis."
                    : result.RejectionReason
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
