using AgroLink.Domain.Entities;

namespace AgroLink.Domain.Interfaces;

public interface ILotRepository : IRepository<Lot>
{
    Task<IEnumerable<Lot>> GetByPaddockIdAsync(
        int paddockId,
        CancellationToken cancellationToken = default
    );

    Task<Lot?> GetLotWithPaddockAsync(int id, CancellationToken cancellationToken = default);

    Task<IEnumerable<Lot>> GetLotsWithPaddockAsync(
        IEnumerable<int> ids,
        CancellationToken cancellationToken = default
    );
}
