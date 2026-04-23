using AgroLink.Domain.Entities;

namespace AgroLink.Domain.Interfaces;

public interface ILotRepository : IRepository<Lot>
{
    Task<IEnumerable<Lot>> GetByPaddockIdAsync(int paddockId);
    Task<Lot?> GetLotWithPaddockAsync(int id);
    Task<IEnumerable<Lot>> GetLotsWithPaddockAsync(IEnumerable<int> ids);
}
