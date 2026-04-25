namespace AgroLink.Application.Interfaces;

public record EntityResolutionResult(
    int? AnimalId,
    int? LotId,
    int? TargetPaddockId,
    int? MotherId
);

public interface IEntityResolutionService
{
    Task<EntityResolutionResult> ResolveAsync(
        int farmId,
        string? animalMention,
        string? lotMention,
        string? targetPaddockMention,
        string? motherMention,
        CancellationToken ct = default
    );
}
