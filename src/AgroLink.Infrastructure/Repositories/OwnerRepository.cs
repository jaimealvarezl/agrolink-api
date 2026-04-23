using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using AgroLink.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AgroLink.Infrastructure.Repositories;

public class OwnerRepository : Repository<Owner>, IOwnerRepository
{
    public OwnerRepository(AgroLinkDbContext context)
        : base(context) { }

    public async Task<IEnumerable<Owner>> GetOwnersByFarmAsync(int farmId)
    {
        return await _dbSet
            .AsNoTracking()
            .Include(o => o.AnimalOwners)
            .Where(o => o.FarmId == farmId)
            .OrderBy(o => o.Name)
            .ToListAsync();
    }
}
