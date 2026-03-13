using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using AgroLink.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AgroLink.Infrastructure.Repositories;

public class ChecklistRepository(AgroLinkDbContext context) : Repository<Checklist>(context), IChecklistRepository
{
    public async Task<IEnumerable<Checklist>> GetByLotIdAsync(int lotId)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(c => c.LotId == lotId)
            .OrderByDescending(c => c.Date)
            .ToListAsync();
    }

    public async Task<Checklist?> GetChecklistWithItemsAsync(int id)
    {
        return await _dbSet
            .AsNoTracking()
            .Include(c => c.ChecklistItems)
                .ThenInclude(ci => ci.Animal)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<IEnumerable<Checklist>> GetByDateRangeAsync(
        DateTime startDate,
        DateTime endDate
    )
    {
        return await _dbSet
            .AsNoTracking()
            .Where(c => c.Date >= startDate && c.Date <= endDate)
            .OrderByDescending(c => c.Date)
            .ToListAsync();
    }
}
