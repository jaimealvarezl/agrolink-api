using AgroLink.Domain.Enums;

namespace AgroLink.Application.Features.ClinicalCases.Models;

public class ClinicalMedicationAdviceResult
{
    public string AdviceText { get; init; } = string.Empty;
    public string Disclaimer { get; init; } = string.Empty;

    public ClinicalRiskLevel RiskLevel { get; init; } = ClinicalRiskLevel.Medium;
    public double ConfidenceScore { get; init; } = 0.5;
    public string RawModelResponse { get; init; } = string.Empty;
}
