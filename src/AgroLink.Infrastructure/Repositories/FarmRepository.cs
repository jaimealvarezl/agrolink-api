using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using AgroLink.Domain.Models;
using AgroLink.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AgroLink.Infrastructure.Repositories;

public class FarmRepository(AgroLinkDbContext context) : Repository<Farm>(context), IFarmRepository
{
    public async Task<IEnumerable<Farm>> GetFarmsWithPaddocksAsync(
        CancellationToken cancellationToken = default
    )
    {
        return await _dbSet.AsNoTracking().Include(f => f.Paddocks).ToListAsync(cancellationToken);
    }

    public async Task<Farm?> GetFarmWithPaddocksAsync(
        int id,
        CancellationToken cancellationToken = default
    )
    {
        return await _dbSet
            .AsNoTracking()
            .Include(f => f.Paddocks)
            .FirstOrDefaultAsync(f => f.Id == id, cancellationToken);
    }

    public async Task<FarmHierarchy?> GetFarmHierarchyAsync(
        int id,
        CancellationToken cancellationToken = default
    )
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
                        Lots = p
                            .Lots.Select(l => new LotHierarchy
                            {
                                Id = l.Id,
                                Name = l.Name,
                                AnimalCount = l.Animals.Count,
                            })
                            .ToList(),
                    })
                    .ToList(),
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Farm?> FindByReferenceAsync(string reference, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(reference))
        {
            return null;
        }

        var normalized = reference.Trim().ToLowerInvariant();

        var exact = await _dbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(
                f =>
                    f.Name.ToLower() == normalized
                    || (f.CUE != null && f.CUE.ToLower() == normalized),
                ct
            );

        if (exact != null)
        {
            return exact;
        }

        return await _dbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(
                f =>
                    f.Name.ToLower().Contains(normalized)
                    || (f.CUE != null && f.CUE.ToLower().Contains(normalized)),
                ct
            );
    }
}
