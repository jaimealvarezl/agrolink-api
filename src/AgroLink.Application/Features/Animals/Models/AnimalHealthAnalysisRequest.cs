using AgroLink.Domain.Enums;

namespace AgroLink.Application.Features.Animals.Models;

public class AnimalHealthAnalysisRequest
{
    public string AnimalName { get; init; } = string.Empty;
    public string? Breed { get; init; }
    public Sex Sex { get; init; }
    public DateTime BirthDate { get; init; }
    public ProductionStatus ProductionStatus { get; init; }
    public ReproductiveStatus ReproductiveStatus { get; init; }
    public string PhotoStorageKey { get; init; } = string.Empty;
    public string PhotoContentType { get; init; } = "image/jpeg";
}
