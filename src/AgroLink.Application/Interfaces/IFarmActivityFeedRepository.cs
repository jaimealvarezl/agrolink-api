using AgroLink.Domain.Entities;

namespace AgroLink.Application.Interfaces;

public interface IFarmActivityFeedRepository
{
    Task<IEnumerable<Movement>> GetFarmMovementsAsync(
        int farmId,
        int limit,
        CancellationToken ct = default
    );

    Task<IEnumerable<AnimalNote>> GetFarmNotesAsync(
        int farmId,
        int limit,
        CancellationToken ct = default
    );

    Task<IEnumerable<AnimalRetirement>> GetFarmRetirementsAsync(
        int farmId,
        int limit,
        CancellationToken ct = default
    );

    Task<IEnumerable<Animal>> GetFarmNewbornsAsync(
        int farmId,
        int limit,
        CancellationToken ct = default
    );
}
