using AgroLink.Domain.Entities;

namespace AgroLink.Domain.Interfaces;

public interface IChecklistRepository : IRepository<Checklist>
{
    Task<IEnumerable<Checklist>> GetByLotIdAsync(
        int lotId,
        CancellationToken cancellationToken = default
    );

    Task<(IEnumerable<Checklist> Items, int TotalCount)> GetPagedByFarmAsync(
        int farmId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default
    );

    Task<IEnumerable<ChecklistItem>> GetItemsByAnimalIdAsync(
        int animalId,
        CancellationToken cancellationToken = default
    );
}
