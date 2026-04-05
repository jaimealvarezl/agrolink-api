namespace AgroLink.Application.Features.ClinicalCases.Models;

public class ClinicalMedicationAdviceRequest
{
    public string Country { get; init; } = "Nicaragua";
    public string Species { get; init; } = "Ganado bovino";
    public string FarmName { get; init; } = string.Empty;
    public string AnimalReference { get; init; } = string.Empty;
    public string EarTag { get; init; } = string.Empty;
    public string SymptomsSummary { get; init; } = string.Empty;
    public string TranscriptText { get; init; } = string.Empty;
}
