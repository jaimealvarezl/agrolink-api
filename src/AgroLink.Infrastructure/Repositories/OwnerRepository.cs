using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using AgroLink.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AgroLink.Infrastructure.Repositories;

public class OwnerRepository(AgroLinkDbContext context)
    : Repository<Owner>(context),
        IOwnerRepository
{
    public async Task<IEnumerable<Owner>> GetOwnersByFarmAsync(
        int farmId,
        CancellationToken cancellationToken = default
    )
    {
        return await _dbSet
            .AsNoTracking()
            .Include(o => o.AnimalOwners)
            .Where(o => o.FarmId == farmId)
            .OrderBy(o => o.Name)
            .ToListAsync(cancellationToken);
    }
}
