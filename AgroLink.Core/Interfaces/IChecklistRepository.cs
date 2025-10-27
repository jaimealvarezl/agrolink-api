using AgroLink.Core.Entities;

namespace AgroLink.Core.Interfaces;

public interface IChecklistRepository : IRepository<Checklist>
{
    Task<IEnumerable<Checklist>> GetByScopeAsync(string scopeType, int scopeId);
    Task<Checklist?> GetChecklistWithItemsAsync(int id);
    Task<IEnumerable<Checklist>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);
}