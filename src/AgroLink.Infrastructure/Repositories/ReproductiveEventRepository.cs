using AgroLink.Domain.Entities;
using AgroLink.Domain.Enums;
using AgroLink.Domain.Interfaces;
using AgroLink.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AgroLink.Infrastructure.Repositories;

public class ReproductiveEventRepository(AgroLinkDbContext context) : IReproductiveEventRepository
{
    public async Task AddAsync(ReproductiveEvent ev, CancellationToken cancellationToken = default)
    {
        await context.ReproductiveEvents.AddAsync(ev, cancellationToken);
    }

    public async Task<ReproductiveEvent?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default
    )
    {
        return await context
            .ReproductiveEvents.AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<ReproductiveEvent>> GetByAnimalIdAsync(
        int animalId,
        CancellationToken cancellationToken = default
    )
    {
        return await context
            .ReproductiveEvents.AsNoTracking()
            .Where(e => e.AnimalId == animalId)
            .OrderByDescending(e => e.Date)
            .ToListAsync(cancellationToken);
    }

    public async Task<ReproductiveEvent?> GetLatestPositivePregnancyOrMatingAsync(
        int animalId,
        CancellationToken cancellationToken = default
    )
    {
        return await context
            .ReproductiveEvents.Where(e =>
                e.AnimalId == animalId
                && e.Status == ReproductiveEventStatus.Positive
                && (
                    e.EventType == ReproductiveEventType.PregnancyCheck
                    || e.EventType == ReproductiveEventType.Mating
                )
            )
            .OrderByDescending(e => e.Date)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
