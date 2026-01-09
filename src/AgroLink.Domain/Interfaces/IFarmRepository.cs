using AgroLink.Domain.Entities;

namespace AgroLink.Domain.Interfaces;

public interface IFarmRepository : IRepository<Farm>
{
    Task<IEnumerable<Farm>> GetFarmsWithPaddocksAsync();
    Task<Farm?> GetFarmWithPaddocksAsync(int id);
    Task<Farm?> GetFarmHierarchyAsync(int id);
}
