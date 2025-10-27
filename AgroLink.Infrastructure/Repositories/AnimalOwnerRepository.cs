using AgroLink.Core.Entities;
using AgroLink.Core.Interfaces;
using AgroLink.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AgroLink.Infrastructure.Repositories;

public class AnimalOwnerRepository : Repository<AnimalOwner>, IAnimalOwnerRepository
{
    public AnimalOwnerRepository(AgroLinkDbContext context)
        : base(context) { }

    public async Task<IEnumerable<AnimalOwner>> GetByAnimalIdAsync(int animalId)
    {
        return await _dbSet
            .Where(ao => ao.AnimalId == animalId)
            .Include(ao => ao.Owner)
            .ToListAsync();
    }

    public async Task<IEnumerable<AnimalOwner>> GetByOwnerIdAsync(int ownerId)
    {
        return await _dbSet
            .Where(ao => ao.OwnerId == ownerId)
            .Include(ao => ao.Animal)
            .ToListAsync();
    }

    public async Task RemoveByAnimalIdAsync(int animalId)
    {
        var animalOwners = await _dbSet.Where(ao => ao.AnimalId == animalId).ToListAsync();

        _dbSet.RemoveRange(animalOwners);
    }
}
