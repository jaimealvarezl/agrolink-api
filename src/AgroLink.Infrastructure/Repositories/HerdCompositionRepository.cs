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
        // LEFT JOIN Animals → AnimalOwners → Owners.
        // Animals with no owners appear once with OwnerName = null.
        var rawRows = await (
            from a in context.Animals.AsNoTracking()
            where a.Lot.Paddock.FarmId == farmId && a.LifeStatus != LifeStatus.Retired
            from ao in a.AnimalOwners.DefaultIfEmpty()
            select new { AnimalId = a.Id, OwnerName = ao != null ? ao.Owner.Name : null }
        ).ToListAsync(ct);

        return rawRows
            .GroupBy(r => r.AnimalId)
            .Select(g => new AnimalOwnerRow(
                g.Key,
                g.Select(r => r.OwnerName).Where(n => n != null).ToList()
            ))
            .ToList();
    }
}
