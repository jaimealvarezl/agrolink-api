using AgroLink.Core.Entities;
using AgroLink.Core.Interfaces;
using AgroLink.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AgroLink.Infrastructure.Repositories;

public class MovementRepository : Repository<Movement>, IMovementRepository
{
    public MovementRepository(AgroLinkDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Movement>> GetByEntityAsync(string entityType, int entityId)
    {
        return await _dbSet
            .Where(m => m.EntityType == entityType && m.EntityId == entityId)
            .OrderByDescending(m => m.At)
            .ToListAsync();
    }

    public async Task<IEnumerable<Movement>> GetAnimalHistoryAsync(int animalId)
    {
        return await GetByEntityAsync("ANIMAL", animalId);
    }

    public async Task<IEnumerable<Movement>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        return await _dbSet
            .Where(m => m.At >= startDate && m.At <= endDate)
            .OrderByDescending(m => m.At)
            .ToListAsync();
    }
}