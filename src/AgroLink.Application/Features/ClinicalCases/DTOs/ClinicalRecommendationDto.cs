using AgroLink.Domain.Enums;

namespace AgroLink.Application.Features.ClinicalCases.DTOs;

public class ClinicalRecommendationDto
{
    public int Id { get; init; }
    public RecommendationSource RecommendationSource { get; init; }
    public string AdviceText { get; init; } = string.Empty;
    public string Disclaimer { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
}
