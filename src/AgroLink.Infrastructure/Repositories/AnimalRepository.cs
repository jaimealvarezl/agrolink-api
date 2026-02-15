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
        var activeStatuses = new[] { LifeStatus.Active, LifeStatus.Missing };

        var query = _dbSet.Where(a =>
            a.Name.ToLower() == name.ToLower()
            && a.Lot.Paddock.FarmId == farmId
            && activeStatuses.Contains(a.LifeStatus)
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
        Sex? sex = null
    )
    {
        var query = _dbSet
            .Include(a => a.Lot)
                .ThenInclude(l => l.Paddock)
            .Include(a => a.Photos)
            .Where(a => a.Lot.Paddock.FarmId == farmId);

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

    public async Task<Animal?> GetAnimalDetailsAsync(int id)
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
            .FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task<List<string>> GetDistinctColorsAsync(string query, int limit = 10)
    {
        return await _dbSet
            .Where(a =>
                !string.IsNullOrEmpty(a.Color) && a.Color.Contains(query, StringComparison.CurrentCultureIgnoreCase)
            )
            .Select(a => a.Color!)
            .Distinct()
            .OrderBy(c => c)
            .Take(limit)
            .ToListAsync();
    }
}
