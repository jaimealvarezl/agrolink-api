using AgroLink.Application.Features.ClinicalCases.DTOs;
using AgroLink.Domain.Interfaces;
using MediatR;

namespace AgroLink.Application.Features.ClinicalCases.Queries.GetLatestReport;

public class GetLatestAnimalClinicalReportQueryHandler(
    IFarmRepository farmRepository,
    IAnimalRepository animalRepository,
    IClinicalCaseRepository clinicalCaseRepository
) : IRequestHandler<GetLatestAnimalClinicalReportQuery, ClinicalLatestReportDto?>
{
    public async Task<ClinicalLatestReportDto?> Handle(
        GetLatestAnimalClinicalReportQuery request,
        CancellationToken cancellationToken
    )
    {
        var farm = await farmRepository.FindByReferenceAsync(
            request.FarmReference,
            cancellationToken
        );
        if (farm == null)
        {
            return null;
        }

        var animal = await animalRepository.GetByEarTagInFarmAsync(
            farm.Id,
            request.EarTag,
            cancellationToken
        );
        if (animal == null)
        {
            return null;
        }

        var clinicalCase = await clinicalCaseRepository.GetLatestByFarmAndAnimalAsync(
            farm.Id,
            animal.Id,
            cancellationToken
        );
        if (clinicalCase == null)
        {
            return null;
        }

        var latestEvent = clinicalCase.Events.OrderByDescending(e => e.CreatedAt).FirstOrDefault();
        var latestRecommendation = clinicalCase
            .Recommendations.OrderByDescending(r => r.CreatedAt)
            .FirstOrDefault();

        return new ClinicalLatestReportDto
        {
            CaseId = clinicalCase.Id,
            FarmId = farm.Id,
            FarmName = farm.Name,
            AnimalId = animal.Id,
            AnimalName = animal.Name,
            EarTag = animal.TagVisual ?? animal.Cuia ?? request.EarTag,
            State = clinicalCase.State,
            RiskLevel = clinicalCase.RiskLevel,
            LastTranscript = latestEvent?.Transcript ?? string.Empty,
            LatestRecommendation =
                latestRecommendation == null
                    ? null
                    : new ClinicalRecommendationDto
                    {
                        Id = latestRecommendation.Id,
                        RecommendationSource = latestRecommendation.RecommendationSource,
                        AdviceText = latestRecommendation.AdviceText,
                        Disclaimer = latestRecommendation.Disclaimer,
                        CreatedAt = latestRecommendation.CreatedAt,
                    },
            OpenedAt = clinicalCase.OpenedAt,
            UpdatedAt = clinicalCase.UpdatedAt,
        };
    }
}
