using AgroLink.Domain.Entities;

namespace AgroLink.Domain.Interfaces;

public interface IReproductiveEventRepository
{
    Task AddAsync(ReproductiveEvent ev, CancellationToken cancellationToken = default);

    Task<ReproductiveEvent?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ReproductiveEvent>> GetByAnimalIdAsync(
        int animalId,
        CancellationToken cancellationToken = default
    );

    Task<ReproductiveEvent?> GetLatestPositivePregnancyOrMatingAsync(
        int animalId,
        CancellationToken cancellationToken = default
    );
}
