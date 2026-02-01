using AgroLink.Domain.Entities;

namespace AgroLink.Domain.Interfaces;

public interface ILotRepository : IRepository<Lot>
{
    Task<IEnumerable<Lot>> GetByPaddockIdAsync(int paddockId);
    Task<Lot?> GetLotWithAnimalsAsync(int id);
    Task<Lot?> GetLotWithPaddockAsync(int id);
}
