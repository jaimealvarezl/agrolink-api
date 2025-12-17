using AgroLink.Domain.Entities;

namespace AgroLink.Domain.Interfaces;

public interface IPaddockRepository : IRepository<Paddock>
{
    Task<IEnumerable<Paddock>> GetByFarmIdAsync(int farmId);
    Task<Paddock?> GetPaddockWithLotsAsync(int id);
}
