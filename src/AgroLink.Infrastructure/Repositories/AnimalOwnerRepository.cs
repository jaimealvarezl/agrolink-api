using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using AgroLink.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AgroLink.Infrastructure.Repositories;

public class AnimalOwnerRepository(AgroLinkDbContext context)
    : Repository<AnimalOwner>(context),
        IAnimalOwnerRepository
{
    public async Task<IEnumerable<AnimalOwner>> GetByAnimalIdAsync(
        int animalId,
        CancellationToken cancellationToken = default
    )
    {
        return await _dbSet
            .AsNoTracking()
            .Where(ao => ao.AnimalId == animalId)
            .Include(ao => ao.Owner)
            .ToListAsync(cancellationToken);
    }

    public async Task RemoveByAnimalIdAsync(
        int animalId,
        CancellationToken cancellationToken = default
    )
    {
        await _dbSet.Where(ao => ao.AnimalId == animalId).ExecuteDeleteAsync(cancellationToken);
    }
}
