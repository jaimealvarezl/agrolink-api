using AgroLink.Application.Interfaces;
using AgroLink.Domain.Enums;
using AgroLink.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AgroLink.Infrastructure.Repositories;

public class HerdCompositionRepository(AgroLinkDbContext context) : IHerdCompositionRepository
{
    public async Task<List<LotSexRow>> GetLotSexGroupsAsync(
        int farmId,
        CancellationToken ct = default
    )
    {
        return await context
            .Animals.AsNoTracking()
            .Where(a => a.Lot.Paddock.FarmId == farmId && a.LifeStatus != LifeStatus.Retired)
            .GroupBy(a => new
            {
                a.LotId,
                LotName = a.Lot.Name,
                a.Sex,
            })
            .Select(g => new LotSexRow(g.Key.LotId, g.Key.LotName, g.Key.Sex, g.Count()))
            .ToListAsync(ct);
    }

    public async Task<List<AnimalOwnerRow>> GetAnimalOwnerRowsAsync(
        int farmId,
        CancellationToken ct = default
    )
    {
        return await context
            .Animals.AsNoTracking()
            .Where(a => a.Lot.Paddock.FarmId == farmId && a.LifeStatus != LifeStatus.Retired)
            .Select(a => new AnimalOwnerRow(
                a.Id,
                a.AnimalOwners.Select(ao => ao.Owner.Name).ToList()
            ))
            .ToListAsync(ct);
    }
}
