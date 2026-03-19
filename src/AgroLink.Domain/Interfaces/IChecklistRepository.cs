using AgroLink.Domain.Entities;

namespace AgroLink.Domain.Interfaces;

public interface IChecklistRepository : IRepository<Checklist>
{
    Task<IEnumerable<Checklist>> GetByLotIdAsync(int lotId);

    Task<(IEnumerable<Checklist> Items, int TotalCount)> GetPagedByFarmAsync(
        int farmId,
        int page,
        int pageSize
    );
}
