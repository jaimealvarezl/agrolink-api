using AgroLink.Domain.Entities;

namespace AgroLink.Domain.Interfaces;

public interface IChecklistRepository : IRepository<Checklist>
{
    Task<IEnumerable<Checklist>> GetByScopeAsync(string scopeType, int scopeId);
    Task<Checklist?> GetChecklistWithItemsAsync(int id);
    Task<IEnumerable<Checklist>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);
}
