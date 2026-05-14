using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using AgroLink.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AgroLink.Infrastructure.Repositories;

public class AnimalRetirementRepository(AgroLinkDbContext context) : IAnimalRetirementRepository
{
    public async Task<AnimalRetirement?> GetByAnimalIdAsync(
        int animalId,
        CancellationToken cancellationToken = default
    )
    {
        return await context
            .AnimalRetirements.AsNoTracking()
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.AnimalId == animalId, cancellationToken);
    }

    public async Task AddAsync(AnimalRetirement retirement, CancellationToken cancellationToken)
    {
        await context.AnimalRetirements.AddAsync(retirement, cancellationToken);
    }
}
