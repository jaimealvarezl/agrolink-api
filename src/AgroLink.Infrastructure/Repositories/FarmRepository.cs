using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using AgroLink.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AgroLink.Infrastructure.Repositories;

public class FarmRepository(AgroLinkDbContext context) : Repository<Farm>(context), IFarmRepository
{
    public async Task<IEnumerable<Farm>> GetFarmsWithPaddocksAsync()
    {
        return await _dbSet.Include(f => f.Paddocks).ToListAsync();
    }

    public async Task<Farm?> GetFarmWithPaddocksAsync(int id)
    {
        return await _dbSet.Include(f => f.Paddocks).FirstOrDefaultAsync(f => f.Id == id);
    }

    public async Task<Farm?> GetFarmHierarchyAsync(int id)
    {
        return await _dbSet
            .Include(f => f.Paddocks)
                .ThenInclude(p => p.Lots)
                    .ThenInclude(l => l.Animals)
            .FirstOrDefaultAsync(f => f.Id == id);
    }
}
