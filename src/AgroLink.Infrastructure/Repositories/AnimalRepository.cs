using AgroLink.Domain.Constants;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Enums;
using AgroLink.Domain.Interfaces;
using AgroLink.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AgroLink.Infrastructure.Repositories;

public class AnimalRepository(AgroLinkDbContext context)
    : Repository<Animal>(context),
        IAnimalRepository
{
    public async Task<IEnumerable<Animal>> GetByLotIdAsync(int lotId, int userId)
    {
        return await _dbSet
            .Include(a => a.Lot)
                .ThenInclude(l => l.Paddock)
            .Where(a =>
                a.LotId == lotId
                && (
                    _context.FarmMembers.Any(m =>
                        m.UserId == userId && m.FarmId == a.Lot.Paddock.FarmId
                    )
                    || _context.Farms.Any(f =>
                        f.Id == a.Lot.Paddock.FarmId && f.Owner != null && f.Owner.UserId == userId
                    )
                )
            )
            .ToListAsync();
    }

    public async Task<Animal?> GetByIdAsync(int id, int userId)
    {
        return await _dbSet
            .Include(a => a.Lot)
                .ThenInclude(l => l.Paddock)
            .Where(a =>
                a.Id == id
                && (
                    _context.FarmMembers.Any(m =>
                        m.UserId == userId && m.FarmId == a.Lot.Paddock.FarmId
                    )
                    || _context.Farms.Any(f =>
                        f.Id == a.Lot.Paddock.FarmId && f.Owner != null && f.Owner.UserId == userId
                    )
                )
            )
            .FirstOrDefaultAsync();
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

    public async Task<bool> IsCuiaUniqueInFarmAsync(
        string cuia,
        int farmId,
        int? excludeAnimalId = null
    )
    {
        var query = _dbSet
            .IgnoreQueryFilters()
            .Where(a =>
                a.Cuia != null
                && a.Cuia.ToLower() == cuia.ToLower()
                && a.Lot.Paddock.FarmId == farmId
            );

        if (excludeAnimalId.HasValue)
        {
            query = query.Where(a => a.Id != excludeAnimalId.Value);
        }

        return !await query.AnyAsync();
    }

    public async Task<bool> IsNameUniqueInFarmAsync(
        string name,
        int farmId,
        int? excludeAnimalId = null
    )
    {
        var query = _dbSet.Where(a =>
            a.Name.ToLower() == name.ToLower()
            && a.Lot.Paddock.FarmId == farmId
            && AnimalConstants.ActiveStatuses.Contains(a.LifeStatus)
        );

        if (excludeAnimalId.HasValue)
        {
            query = query.Where(a => a.Id != excludeAnimalId.Value);
        }

        return !await query.AnyAsync();
    }

    public async Task<(IEnumerable<Animal> Items, int TotalCount)> GetPagedListAsync(
        int farmId,
        int page,
        int pageSize,
        int? lotId = null,
        string? searchTerm = null,
        bool isSick = false,
        bool isPregnant = false,
        bool isMissing = false,
        Sex? sex = null,
        bool includeRetired = false
    )
    {
        var query = _dbSet
            .Include(a => a.Lot)
                .ThenInclude(l => l.Paddock)
            .Include(a => a.Photos)
            .Where(a => a.Lot.Paddock.FarmId == farmId);

        query = includeRetired
            ? query.Where(a => !AnimalConstants.ActiveStatuses.Contains(a.LifeStatus))
            : query.Where(a => AnimalConstants.ActiveStatuses.Contains(a.LifeStatus));

        if (lotId.HasValue)
        {
            query = query.Where(a => a.LotId == lotId.Value);
        }

        if (sex.HasValue)
        {
            query = query.Where(a => a.Sex == sex.Value);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.ToLower();
            query = query.Where(a =>
                (a.TagVisual != null && a.TagVisual.ToLower().Contains(term))
                || a.Name.ToLower().Contains(term)
                || (a.Cuia != null && a.Cuia.ToLower().Contains(term))
            );
        }

        if (isSick)
        {
            query = query.Where(a => a.HealthStatus == HealthStatus.Sick);
        }

        if (isPregnant)
        {
            query = query.Where(a => a.ReproductiveStatus == ReproductiveStatus.Pregnant);
        }

        if (isMissing)
        {
            query = query.Where(a => a.LifeStatus == LifeStatus.Missing);
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(a => a.Name)
            .ThenBy(a => a.TagVisual)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<IEnumerable<Animal>> GetAllByFarmAsync(
        int farmId,
        CancellationToken cancellationToken = default
    )
    {
        return await _dbSet
            .Include(a => a.Lot)
            .Include(a => a.Mother)
            .Include(a => a.Father)
            .Include(a => a.AnimalOwners)
                .ThenInclude(ao => ao.Owner)
            .Include(a => a.Photos)
            .Where(a => a.Lot.Paddock.FarmId == farmId)
            .ToListAsync(cancellationToken);
    }

    public async Task<Animal?> GetAnimalDetailsAsync(int id, int userId)
    {
        return await _dbSet
            .Include(a => a.Lot)
                .ThenInclude(l => l.Paddock)
            .Include(a => a.Mother)
                .ThenInclude(m => m!.Photos)
            .Include(a => a.Father)
                .ThenInclude(f => f!.Photos)
            .Include(a => a.AnimalOwners)
                .ThenInclude(ao => ao.Owner)
            .Include(a => a.Photos)
            .Where(a =>
                _context.FarmMembers.Any(m =>
                    m.UserId == userId && m.FarmId == a.Lot.Paddock.FarmId
                )
                || _context.Farms.Any(f =>
                    f.Id == a.Lot.Paddock.FarmId && f.Owner != null && f.Owner.UserId == userId
                )
            )
            .FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task<Animal?> GetLotWithPaddockAsync(int id)
    {
        return await _dbSet
            .Include(a => a.Lot)
                .ThenInclude(l => l.Paddock)
            .FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task<Animal?> GetByIdInFarmAsync(
        int id,
        int farmId,
        CancellationToken ct = default
    )
    {
        return await _dbSet.FirstOrDefaultAsync(
            a => a.Id == id && a.Lot.Paddock.FarmId == farmId,
            ct
        );
    }

    public async Task<List<string>> GetDistinctColorsAsync(
        int farmId,
        CancellationToken cancellationToken = default
    )
    {
        return await _dbSet
            .Include(a => a.Lot)
                .ThenInclude(l => l.Paddock)
            .Where(a => !string.IsNullOrEmpty(a.Color) && a.Lot.Paddock.FarmId == farmId)
            .Select(a => a.Color!)
            .GroupBy(c => c.ToLower())
            .Select(g => g.OrderBy(c => c).First())
            .OrderBy(c => c)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<string>> GetDistinctBreedsAsync(
        int farmId,
        CancellationToken cancellationToken = default
    )
    {
        return await _dbSet
            .Include(a => a.Lot)
                .ThenInclude(l => l.Paddock)
            .Where(a => !string.IsNullOrEmpty(a.Breed) && a.Lot.Paddock.FarmId == farmId)
            .Select(a => a.Breed!)
            .GroupBy(b => b.ToLower())
            .Select(g => g.OrderBy(b => b).First())
            .OrderBy(b => b)
            .ToListAsync(cancellationToken);
    }
}
