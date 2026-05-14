namespace AgroLink.Application.Features.Animals.DTOs;

public class AnimalHealthAnalysisDto
{
    public double EstimatedBcs { get; init; }
    public bool HasAlerts { get; init; }
    public string? AlertDescription { get; init; }
}
