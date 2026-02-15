using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
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
            .AsNoTracking()
            .Where(ao => ao.AnimalId == animalId)
            .Include(ao => ao.Owner)
            .ToListAsync();
    }

    public async Task<IEnumerable<AnimalOwner>> GetByOwnerIdAsync(int ownerId)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(ao => ao.OwnerId == ownerId)
            .Include(ao => ao.Animal)
            .ToListAsync();
    }

    public async Task RemoveByAnimalIdAsync(int animalId)
    {
        await _dbSet.Where(ao => ao.AnimalId == animalId).ExecuteDeleteAsync();
    }
}
