using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using AgroLink.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AgroLink.Infrastructure.Repositories;

public class PaddockRepository(AgroLinkDbContext context)
    : Repository<Paddock>(context),
        IPaddockRepository
{
    public async Task<IEnumerable<Paddock>> GetByFarmIdAsync(int farmId)
    {
        return await _dbSet.AsNoTracking().Where(p => p.FarmId == farmId).ToListAsync();
    }
}
