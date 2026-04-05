using AgroLink.Application.Features.ClinicalCases.DTOs;
using AgroLink.Domain.Interfaces;
using MediatR;

namespace AgroLink.Application.Features.ClinicalCases.Queries.GetById;

public class GetClinicalCaseByIdQueryHandler(IClinicalCaseRepository clinicalCaseRepository)
    : IRequestHandler<GetClinicalCaseByIdQuery, ClinicalCaseDetailDto?>
{
    public async Task<ClinicalCaseDetailDto?> Handle(
        GetClinicalCaseByIdQuery request,
        CancellationToken cancellationToken
    )
    {
        var clinicalCase = await clinicalCaseRepository.GetByIdWithDetailsAsync(
            request.CaseId,
            cancellationToken
        );
        if (clinicalCase == null)
        {
            return null;
        }

        return new ClinicalCaseDetailDto
        {
            Id = clinicalCase.Id,
            FarmId = clinicalCase.FarmId,
            FarmName = clinicalCase.Farm.Name,
            AnimalId = clinicalCase.AnimalId,
            AnimalName = clinicalCase.Animal?.Name ?? string.Empty,
            EarTag = clinicalCase.EarTag ?? clinicalCase.Animal?.TagVisual ?? string.Empty,
            State = clinicalCase.State,
            RiskLevel = clinicalCase.RiskLevel,
            OpenedAt = clinicalCase.OpenedAt,
            ClosedAt = clinicalCase.ClosedAt,
            Events = clinicalCase
                .Events.OrderByDescending(e => e.CreatedAt)
                .Select(e => new ClinicalCaseEventDto
                {
                    Id = e.Id,
                    EventType = e.EventType,
                    Transcript = e.Transcript,
                    Confidence = e.Confidence,
                    CreatedAt = e.CreatedAt,
                })
                .ToList(),
            Recommendations = clinicalCase
                .Recommendations.OrderByDescending(r => r.CreatedAt)
                .Select(r => new ClinicalRecommendationDto
                {
                    Id = r.Id,
                    RecommendationSource = r.RecommendationSource,
                    AdviceText = r.AdviceText,
                    Disclaimer = r.Disclaimer,
                    CreatedAt = r.CreatedAt,
                })
                .ToList(),
        };
    }
}
