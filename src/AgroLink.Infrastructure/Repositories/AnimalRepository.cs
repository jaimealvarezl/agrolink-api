using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using AgroLink.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AgroLink.Infrastructure.Repositories;

public class AnimalRepository(AgroLinkDbContext context) : Repository<Animal>(context), IAnimalRepository
{
    public async Task<IEnumerable<Animal>> GetByLotIdAsync(int lotId)
    {
        return await _dbSet.Where(a => a.LotId == lotId).ToListAsync();
    }

    public async Task<Animal?> GetAnimalWithOwnersAsync(int id)
    {
        return await _dbSet
            .Include(a => a.AnimalOwners)
                .ThenInclude(ao => ao.Owner)
            .FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task<Animal?> GetAnimalWithGenealogyAsync(int id)
    {
        return await _dbSet
            .Include(a => a.Mother)
            .Include(a => a.Father)
            .Include(a => a.Children)
            .FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task<IEnumerable<Animal>> GetChildrenAsync(int parentId)
    {
        return await _dbSet
            .Where(a => a.MotherId == parentId || a.FatherId == parentId)
            .ToListAsync();
    }

    public async Task<Animal?> GetByCuiaAsync(string cuia)
    {
        return await _dbSet.FirstOrDefaultAsync(a => a.Cuia == cuia);
    }

    public async Task<bool> IsCuiaUniqueInFarmAsync(string cuia, int farmId, int? excludeAnimalId = null)
    {
        var query = _dbSet.Where(a => a.Cuia == cuia && a.Lot.Paddock.FarmId == farmId);

        if (excludeAnimalId.HasValue)
        {
            query = query.Where(a => a.Id != excludeAnimalId.Value);
        }

        return !await query.AnyAsync();
    }
}
