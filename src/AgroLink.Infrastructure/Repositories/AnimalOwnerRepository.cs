using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using AgroLink.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AgroLink.Infrastructure.Repositories;

public class AnimalOwnerRepository(AgroLinkDbContext context)
    : Repository<AnimalOwner>(context),
        IAnimalOwnerRepository
{
    public async Task<IEnumerable<AnimalOwner>> GetByAnimalIdAsync(int animalId)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(ao => ao.AnimalId == animalId)
            .Include(ao => ao.Owner)
            .ToListAsync();
    }

    public async Task RemoveByAnimalIdAsync(int animalId)
    {
        await _dbSet.Where(ao => ao.AnimalId == animalId).ExecuteDeleteAsync();
    }
}
