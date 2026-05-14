using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using AgroLink.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AgroLink.Infrastructure.Repositories;

public class LotRepository(AgroLinkDbContext context) : Repository<Lot>(context), ILotRepository
{
    public async Task<IEnumerable<Lot>> GetByPaddockIdAsync(
        int paddockId,
        CancellationToken cancellationToken = default
    )
    {
        return await _dbSet.Where(l => l.PaddockId == paddockId).ToListAsync(cancellationToken);
    }

    public async Task<Lot?> GetLotWithPaddockAsync(
        int id,
        CancellationToken cancellationToken = default
    )
    {
        return await _dbSet
            .Include(l => l.Paddock)
            .FirstOrDefaultAsync(l => l.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<Lot>> GetLotsWithPaddockAsync(
        IEnumerable<int> ids,
        CancellationToken cancellationToken = default
    )
    {
        return await _dbSet
            .Include(l => l.Paddock)
            .Where(l => ids.Contains(l.Id))
            .ToListAsync(cancellationToken);
    }
}
