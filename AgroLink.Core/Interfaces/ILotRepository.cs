using AgroLink.Core.Entities;

namespace AgroLink.Core.Interfaces;

public interface ILotRepository : IRepository<Lot>
{
    Task<IEnumerable<Lot>> GetByPaddockIdAsync(int paddockId);
    Task<Lot?> GetLotWithAnimalsAsync(int id);
}
