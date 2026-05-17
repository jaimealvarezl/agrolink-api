using AgroLink.Application.Interfaces;
using AgroLink.Domain.Entities;
using AgroLink.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AgroLink.Infrastructure.Repositories;

public class FarmActivityFeedRepository(AgroLinkDbContext context) : IFarmActivityFeedRepository
{
    public async Task<IEnumerable<Movement>> GetFarmMovementsAsync(
        int farmId,
        int limit,
        CancellationToken ct = default
    )
    {
        return await context
            .Movements.AsNoTracking()
            .Include(m => m.Animal)
            .Include(m => m.ToLot)
            .Where(m => m.Animal.Lot.Paddock.FarmId == farmId)
            .OrderByDescending(m => m.At)
            .Take(limit)
            .ToListAsync(ct);
    }

    public async Task<IEnumerable<AnimalNote>> GetFarmNotesAsync(
        int farmId,
        int limit,
        CancellationToken ct = default
    )
    {
        return await context
            .AnimalNotes.AsNoTracking()
            .Include(n => n.Animal)
            .Where(n => n.Animal.Lot.Paddock.FarmId == farmId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(limit)
            .ToListAsync(ct);
    }

    public async Task<IEnumerable<AnimalRetirement>> GetFarmRetirementsAsync(
        int farmId,
        int limit,
        CancellationToken ct = default
    )
    {
        return await context
            .AnimalRetirements.AsNoTracking()
            .Include(r => r.Animal)
            .Where(r => r.Animal.Lot.Paddock.FarmId == farmId)
            .OrderByDescending(r => r.At)
            .Take(limit)
            .ToListAsync(ct);
    }

    public async Task<IEnumerable<Animal>> GetFarmNewbornsAsync(
        int farmId,
        int limit,
        CancellationToken ct = default
    )
    {
        return await context
            .Animals.AsNoTracking()
            .Where(a => a.Lot.Paddock.FarmId == farmId && a.MotherId != null)
            .OrderByDescending(a => a.BirthDate)
            .Take(limit)
            .ToListAsync(ct);
    }
}
