using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using AgroLink.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AgroLink.Infrastructure.Repositories;

public class LotRepository(AgroLinkDbContext context) : Repository<Lot>(context), ILotRepository
{
    public async Task<IEnumerable<Lot>> GetByPaddockIdAsync(int paddockId)
    {
        return await _dbSet.Where(l => l.PaddockId == paddockId).ToListAsync();
    }

    public async Task<Lot?> GetLotWithAnimalsAsync(int id)
    {
        return await _dbSet.Include(l => l.Animals).FirstOrDefaultAsync(l => l.Id == id);
    }

    public async Task<Lot?> GetLotWithPaddockAsync(int id)
    {
        return await _dbSet.Include(l => l.Paddock).FirstOrDefaultAsync(l => l.Id == id);
    }
}
