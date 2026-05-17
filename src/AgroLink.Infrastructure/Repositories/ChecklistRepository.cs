using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using AgroLink.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AgroLink.Infrastructure.Repositories;

public class ChecklistRepository(AgroLinkDbContext context)
    : Repository<Checklist>(context),
        IChecklistRepository
{
    public async Task<IEnumerable<Checklist>> GetByLotIdAsync(
        int lotId,
        CancellationToken cancellationToken = default
    )
    {
        return await _dbSet
            .AsNoTracking()
            .Where(c => c.LotId == lotId)
            .OrderByDescending(c => c.Date)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<ChecklistItem>> GetItemsByAnimalIdAsync(
        int animalId,
        CancellationToken cancellationToken = default
    )
    {
        return await _context
            .ChecklistItems.AsNoTracking()
            .Include(ci => ci.Checklist)
                .ThenInclude(c => c.Lot)
            .Where(ci => ci.AnimalId == animalId)
            .OrderByDescending(ci => ci.Checklist.Date)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Checklist>> GetLatestPerLotAsync(
        IEnumerable<int> lotIds,
        CancellationToken cancellationToken = default
    )
    {
        var lotIdList = lotIds.ToList();
        if (lotIdList.Count == 0)
        {
            return [];
        }

        // Find the most recent CreatedAt per lot (single aggregate query)
        var latestDates = await _dbSet
            .Where(c => lotIdList.Contains(c.LotId))
            .GroupBy(c => c.LotId)
            .Select(g => new { LotId = g.Key, MaxCreatedAt = g.Max(c => c.CreatedAt) })
            .ToListAsync(cancellationToken);

        if (latestDates.Count == 0)
        {
            return [];
        }

        // Fetch only checklists on or after the earliest of those dates (bounded window)
        var minDate = latestDates.Min(x => x.MaxCreatedAt);
        var candidates = await _dbSet
            .AsNoTracking()
            .Where(c => lotIdList.Contains(c.LotId) && c.CreatedAt >= minDate)
            .ToListAsync(cancellationToken);

        // Pick the latest per lot in memory
        return candidates.GroupBy(c => c.LotId).Select(g => g.MaxBy(c => c.CreatedAt)!).ToList();
    }

    public async Task<(IEnumerable<Checklist> Items, int TotalCount)> GetPagedByFarmAsync(
        int farmId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default
    )
    {
        var lotIds = await _context
            .Lots.Where(l => l.Paddock.FarmId == farmId)
            .Select(l => l.Id)
            .ToListAsync(cancellationToken);

        var query = _dbSet
            .AsNoTracking()
            .Where(c => lotIds.Contains(c.LotId))
            .OrderByDescending(c => c.Date);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
}
