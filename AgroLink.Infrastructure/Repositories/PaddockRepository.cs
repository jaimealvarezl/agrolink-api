using AgroLink.Core.Entities;
using AgroLink.Core.Interfaces;
using AgroLink.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AgroLink.Infrastructure.Repositories;

public class PaddockRepository : Repository<Paddock>, IPaddockRepository
{
    public PaddockRepository(AgroLinkDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Paddock>> GetByFarmIdAsync(int farmId)
    {
        return await _dbSet
            .Where(p => p.FarmId == farmId)
            .ToListAsync();
    }

    public async Task<Paddock?> GetPaddockWithLotsAsync(int id)
    {
        return await _dbSet
            .Include(p => p.Lots)
            .FirstOrDefaultAsync(p => p.Id == id);
    }
}