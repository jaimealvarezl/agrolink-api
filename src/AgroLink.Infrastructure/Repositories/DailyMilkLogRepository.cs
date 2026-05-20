using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using AgroLink.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AgroLink.Infrastructure.Repositories;

public class DailyMilkLogRepository(AgroLinkDbContext context) : IDailyMilkLogRepository
{
    private readonly DbSet<DailyMilkLog> _dbSet = context.Set<DailyMilkLog>();

    public async Task<DailyMilkLog?> FindByDateAsync(
        int farmId,
        DateOnly date,
        CancellationToken ct = default
    )
    {
        return await _dbSet.FirstOrDefaultAsync(l => l.FarmId == farmId && l.Date == date, ct);
    }

    public async Task<(IEnumerable<DailyMilkLog> Items, int TotalCount)> GetPagedByDateRangeAsync(
        int farmId,
        DateOnly from,
        DateOnly to,
        int page,
        int pageSize,
        CancellationToken ct = default
    )
    {
        var query = _dbSet
            .Where(l => l.FarmId == farmId && l.Date >= from && l.Date <= to)
            .OrderByDescending(l => l.Date);

        var totalCount = await query.CountAsync(ct);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);

        return (items, totalCount);
    }

    public async Task<decimal?> FindLastPricePerLiterAsync(
        int farmId,
        CancellationToken ct = default
    )
    {
        return await _dbSet
            .Where(l => l.FarmId == farmId && l.PricePerLiter != null)
            .OrderByDescending(l => l.Date)
            .Select(l => l.PricePerLiter)
            .FirstOrDefaultAsync(ct);
    }

    public async Task AddAsync(DailyMilkLog entity, CancellationToken ct = default)
    {
        await _dbSet.AddAsync(entity, ct);
    }

    public void Update(DailyMilkLog entity)
    {
        _dbSet.Update(entity);
    }
}
