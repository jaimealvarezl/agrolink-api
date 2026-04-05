using AgroLink.Domain.Entities;

namespace AgroLink.Application.Features.ClinicalCases.Models;

public class FarmAnimalResolutionResult
{
    public Farm? Farm { get; init; }
    public Animal? Animal { get; init; }
    public string ResolutionMessage { get; init; } = string.Empty;

    public bool IsResolved => Farm != null && Animal != null;
}
