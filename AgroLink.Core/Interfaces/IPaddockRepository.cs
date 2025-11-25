using AgroLink.Core.Entities;

namespace AgroLink.Core.Interfaces;

public interface IPaddockRepository : IRepository<Paddock>
{
    Task<IEnumerable<Paddock>> GetByFarmIdAsync(int farmId);
    Task<Paddock?> GetPaddockWithLotsAsync(int id);
}
