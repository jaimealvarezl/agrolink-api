using AgroLink.Domain.Entities;

namespace AgroLink.Domain.Interfaces;

public interface IChecklistRepository : IRepository<Checklist>
{
    Task<IEnumerable<Checklist>> GetByLotIdAsync(int lotId);
    Task<Checklist?> GetChecklistWithItemsAsync(int id);
    Task<IEnumerable<Checklist>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);
}
