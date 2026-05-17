using AgroLink.Domain.Entities;

namespace AgroLink.Application.Interfaces;

public interface IFarmActivityFeedRepository
{
    Task<IEnumerable<Movement>> GetFarmMovementsAsync(int farmId, CancellationToken ct = default);

    Task<IEnumerable<AnimalNote>> GetFarmNotesAsync(int farmId, CancellationToken ct = default);

    Task<IEnumerable<AnimalRetirement>> GetFarmRetirementsAsync(
        int farmId,
        CancellationToken ct = default
    );

    Task<IEnumerable<Animal>> GetFarmNewbornsAsync(int farmId, CancellationToken ct = default);
}
