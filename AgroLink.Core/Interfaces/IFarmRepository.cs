using AgroLink.Core.Entities;

namespace AgroLink.Core.Interfaces;

public interface IFarmRepository : IRepository<Farm>
{
    Task<IEnumerable<Farm>> GetFarmsWithPaddocksAsync();
    Task<Farm?> GetFarmWithPaddocksAsync(int id);
}