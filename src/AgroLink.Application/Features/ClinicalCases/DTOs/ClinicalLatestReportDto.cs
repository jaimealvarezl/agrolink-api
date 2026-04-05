using AgroLink.Domain.Enums;

namespace AgroLink.Application.Features.ClinicalCases.DTOs;

public class ClinicalLatestReportDto
{
    public int CaseId { get; init; }
    public int FarmId { get; init; }
    public string FarmName { get; init; } = string.Empty;
    public int AnimalId { get; init; }
    public string AnimalName { get; init; } = string.Empty;
    public string EarTag { get; init; } = string.Empty;
    public ClinicalCaseState State { get; init; }
    public ClinicalRiskLevel RiskLevel { get; init; }
    public string LastTranscript { get; init; } = string.Empty;
    public ClinicalRecommendationDto? LatestRecommendation { get; init; }
    public DateTime OpenedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}
