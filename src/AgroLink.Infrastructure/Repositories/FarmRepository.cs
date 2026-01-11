using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using AgroLink.Domain.Models;
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

    public async Task<FarmHierarchy?> GetFarmHierarchyAsync(int id)
    {
        return await _dbSet
            .Where(f => f.Id == id)
            .Select(f => new FarmHierarchy
            {
                Id = f.Id,
                Name = f.Name,
                Paddocks = f
                    .Paddocks.Select(p => new PaddockHierarchy
                    {
                        Id = p.Id,
                        Name = p.Name,
                                            Lots = p.Lots.Select(l => new LotHierarchy
                                            {
                                                Id = l.Id,
                                                Name = l.Name,
                                                AnimalCount = l.Animals.Count
                                            }).ToList()                    })
                    .ToList(),
            })
            .FirstOrDefaultAsync();
    }
}
