using AgroLink.Core.Entities;
using AgroLink.Core.Interfaces;
using AgroLink.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AgroLink.Infrastructure.Repositories;

public class LotRepository : Repository<Lot>, ILotRepository
{
    public LotRepository(AgroLinkDbContext context)
        : base(context) { }

    public async Task<IEnumerable<Lot>> GetByPaddockIdAsync(int paddockId)
    {
        return await _dbSet.Where(l => l.PaddockId == paddockId).ToListAsync();
    }

    public async Task<Lot?> GetLotWithAnimalsAsync(int id)
    {
        return await _dbSet.Include(l => l.Animals).FirstOrDefaultAsync(l => l.Id == id);
    }
}
