using AgroLink.Domain.Entities;

namespace AgroLink.Application.Interfaces;

public record EntityResolutionResult(
    Animal? Animal = null,
    Lot? Lot = null,
    Paddock? TargetPaddock = null,
    Animal? Mother = null,
    IReadOnlyList<Owner>? Owners = null
);

public interface IEntityResolutionService
{
    Task<EntityResolutionResult> ResolveAsync(
        int farmId,
        string? animalMention,
        string? lotMention,
        string? targetPaddockMention,
        string? motherMention,
        string[]? ownerMentions = null,
        CancellationToken ct = default
    );
}
