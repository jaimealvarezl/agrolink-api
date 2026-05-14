namespace AgroLink.Application.Features.Animals.Models;

public class AnimalHealthAnalysisResult
{
    public double BodyConditionScore { get; init; }
    public bool HasAlert { get; init; }
    public string? AlertDescription { get; init; }
    public bool PhotoRejected { get; init; }
    public string? RejectionReason { get; init; }
    public string RawAiResponse { get; init; } = string.Empty;
}
