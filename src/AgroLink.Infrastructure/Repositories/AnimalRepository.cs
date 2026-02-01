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
        var query = _dbSet.Where(a => a.Cuia == cuia && a.Lot.Paddock.FarmId == farmId);

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
        bool isMissing = false
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

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.ToLower();
            query = query.Where(a =>
                a.TagVisual.Contains(term, StringComparison.InvariantCultureIgnoreCase)
                || (
                    a.Name != null
                    && a.Name.Contains(term, StringComparison.InvariantCultureIgnoreCase)
                )
                || (
                    a.Cuia != null
                    && a.Cuia.Contains(term, StringComparison.InvariantCultureIgnoreCase)
                )
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
            .OrderBy(a => a.TagVisual)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<Animal?> GetAnimalDetailsAsync(int id)
    {
        return await _dbSet
            .Include(a => a.Lot)
            .Include(a => a.Mother)
            .Include(a => a.Father)
            .Include(a => a.AnimalOwners)
                .ThenInclude(ao => ao.Owner)
            .Include(a => a.Photos)
            .FirstOrDefaultAsync(a => a.Id == id);
    }
}
