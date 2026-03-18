using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using AgroLink.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AgroLink.Infrastructure.Repositories;

public class ChecklistRepository(AgroLinkDbContext context)
    : Repository<Checklist>(context),
        IChecklistRepository
{
    public async Task<IEnumerable<Checklist>> GetByLotIdAsync(int lotId)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(c => c.LotId == lotId)
            .OrderByDescending(c => c.Date)
            .ToListAsync();
    }

    public async Task<(IEnumerable<Checklist> Items, int TotalCount)> GetPagedByFarmAsync(
        int farmId,
        int page,
        int pageSize
    )
    {
        var lotIds = await _context
            .Lots.Where(l => l.Paddock.FarmId == farmId)
            .Select(l => l.Id)
            .ToListAsync();

        var query = _dbSet
            .AsNoTracking()
            .Where(c => lotIds.Contains(c.LotId))
            .OrderByDescending(c => c.Date);

        var totalCount = await query.CountAsync();
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        return (items, totalCount);
    }
}
