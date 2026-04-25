using AgroLink.Application.Features.VoiceCommands.DTOs;
using AgroLink.Application.Interfaces;
using AgroLink.Domain.Enums;
using AgroLink.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace AgroLink.Infrastructure.Services;

public class FarmRosterService(
    AgroLinkDbContext context,
    IMemoryCache cache,
    ILogger<FarmRosterService> logger
) : IFarmRosterService
{
    private static readonly TimeSpan _cacheTtl = TimeSpan.FromMinutes(5);

    public async Task<FarmRosterDto> GetRosterAsync(int farmId, CancellationToken ct = default)
    {
        var cacheKey = $"farm_roster:{farmId}";

        if (cache.TryGetValue(cacheKey, out FarmRosterDto? cached) && cached != null)
        {
            return cached;
        }

        logger.LogDebug("Farm roster cache miss for farm {FarmId}, querying database.", farmId);

        var animals = await context
            .Animals.Where(a => a.Lot.Paddock.FarmId == farmId && a.LifeStatus == LifeStatus.Active)
            .OrderByDescending(a => a.UpdatedAt)
            .Take(500)
            .Select(a => new AnimalRosterEntry(
                a.Id,
                a.Name,
                a.TagVisual,
                a.Cuia,
                a.LotId,
                a.Lot.Name
            ))
            .ToListAsync(ct);

        var lots = await context
            .Lots.Where(l => l.Paddock.FarmId == farmId && l.Status == "ACTIVE")
            .Select(l => new LotRosterEntry(l.Id, l.Name, l.PaddockId, l.Paddock.Name))
            .ToListAsync(ct);

        var roster = new FarmRosterDto(animals, lots);
        cache.Set(cacheKey, roster, _cacheTtl);

        logger.LogDebug(
            "Farm roster cached for farm {FarmId}: {AnimalCount} animals, {LotCount} lots.",
            farmId,
            animals.Count,
            lots.Count
        );

        return roster;
    }
}
