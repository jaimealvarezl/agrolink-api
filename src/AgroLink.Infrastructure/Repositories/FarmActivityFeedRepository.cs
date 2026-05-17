using AgroLink.Application.Interfaces;
using AgroLink.Domain.Entities;
using AgroLink.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AgroLink.Infrastructure.Repositories;

public class FarmActivityFeedRepository(AgroLinkDbContext context) : IFarmActivityFeedRepository
{
    public async Task<IEnumerable<Movement>> GetFarmMovementsAsync(
        int farmId,
        CancellationToken ct = default
    )
    {
        return await context
            .Movements.AsNoTracking()
            .Include(m => m.Animal)
            .Include(m => m.ToLot)
            .Where(m => m.Animal.Lot.Paddock.FarmId == farmId)
            .ToListAsync(ct);
    }

    public async Task<IEnumerable<AnimalNote>> GetFarmNotesAsync(
        int farmId,
        CancellationToken ct = default
    )
    {
        return await context
            .AnimalNotes.AsNoTracking()
            .Include(n => n.Animal)
            .Where(n => n.Animal.Lot.Paddock.FarmId == farmId)
            .ToListAsync(ct);
    }

    public async Task<IEnumerable<AnimalRetirement>> GetFarmRetirementsAsync(
        int farmId,
        CancellationToken ct = default
    )
    {
        return await context
            .AnimalRetirements.AsNoTracking()
            .Include(r => r.Animal)
            .Where(r => r.Animal.Lot.Paddock.FarmId == farmId)
            .ToListAsync(ct);
    }

    public async Task<IEnumerable<Animal>> GetFarmNewbornsAsync(
        int farmId,
        CancellationToken ct = default
    )
    {
        return await context
            .Animals.AsNoTracking()
            .Where(a => a.Lot.Paddock.FarmId == farmId && a.MotherId != null)
            .ToListAsync(ct);
    }
}
