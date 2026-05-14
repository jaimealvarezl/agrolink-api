using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using AgroLink.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AgroLink.Infrastructure.Repositories;

public class AnimalBcsReadingRepository(AgroLinkDbContext context) : IAnimalBcsReadingRepository
{
    public async Task AddAsync(
        AnimalBcsReading reading,
        CancellationToken cancellationToken = default
    )
    {
        await context.AnimalBcsReadings.AddAsync(reading, cancellationToken);
    }

    public async Task<AnimalBcsReading?> GetMostRecentByAnimalIdAsync(
        int animalId,
        CancellationToken cancellationToken = default
    )
    {
        return await context
            .AnimalBcsReadings.Where(r => r.AnimalId == animalId)
            .OrderByDescending(r => r.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
