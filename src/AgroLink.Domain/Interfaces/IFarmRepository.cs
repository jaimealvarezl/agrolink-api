using AgroLink.Domain.Entities;
using AgroLink.Domain.Models;

namespace AgroLink.Domain.Interfaces;

public interface IFarmRepository : IRepository<Farm>
{
    Task<IEnumerable<Farm>> GetFarmsWithPaddocksAsync();
    Task<Farm?> GetFarmWithPaddocksAsync(int id);
    Task<FarmHierarchy?> GetFarmHierarchyAsync(int id);
}
