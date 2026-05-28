using AgroLink.Domain.Entities;

namespace AgroLink.Domain.Interfaces;

public interface ITagRepository : IRepository<Tag>
{
    Task<Tag> UpsertAsync(
        int farmId,
        string displayName,
        int userId,
        CancellationToken cancellationToken = default
    );

    Task<List<Tag>> GetByFarmAsync(
        int farmId,
        string? search = null,
        CancellationToken cancellationToken = default
    );

    Task<List<Tag>> GetByCanonicalNamesAsync(
        int farmId,
        IEnumerable<string> canonicalNames,
        CancellationToken cancellationToken = default
    );

    Task<Tag?> RenameAsync(
        int id,
        string displayName,
        CancellationToken cancellationToken = default
    );

    Task<Tag?> UpdateColorAsync(
        int id,
        string? colorToken,
        CancellationToken cancellationToken = default
    );

    Task<(Tag? Tag, int AffectedAnimals)> DeleteWithCascadeAsync(
        int id,
        CancellationToken cancellationToken = default
    );
}
