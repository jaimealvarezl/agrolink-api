using AgroLink.Domain.Enums;

namespace AgroLink.Application.Features.ClinicalCases.Models;

public class ClinicalExtractionResult
{
    public ClinicalMessageIntent Intent { get; init; } = ClinicalMessageIntent.NewCaseReport;
    public string? FarmReference { get; init; }
    public string? AnimalReference { get; init; }
    public string? EarTag { get; init; }
    public string SymptomsSummary { get; init; } = string.Empty;
    public ExtractionConfidenceLevel ConfidenceLevel { get; init; } =
        ExtractionConfidenceLevel.Medium;
    public double ConfidenceScore { get; init; } = 0.5;
}
