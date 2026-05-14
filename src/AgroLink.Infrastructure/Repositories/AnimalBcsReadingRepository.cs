using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using AgroLink.Infrastructure.Data;

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
}
