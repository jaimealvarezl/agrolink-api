using AgroLink.Domain.Entities;
using AgroLink.Domain.Models;

namespace AgroLink.Domain.Interfaces;

public interface IFarmRepository : IRepository<Farm>
{
    Task<IEnumerable<Farm>> GetFarmsWithPaddocksAsync(
        CancellationToken cancellationToken = default
    );

    Task<Farm?> GetFarmWithPaddocksAsync(int id, CancellationToken cancellationToken = default);

    Task<FarmHierarchy?> GetFarmHierarchyAsync(
        int id,
        CancellationToken cancellationToken = default
    );

    Task<Farm?> FindByReferenceAsync(string reference, CancellationToken ct = default);
}
