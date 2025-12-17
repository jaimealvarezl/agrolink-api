using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using AgroLink.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AgroLink.Infrastructure.Repositories;

public class OwnerRepository : Repository<Owner>, IOwnerRepository
{
    public OwnerRepository(AgroLinkDbContext context)
        : base(context) { }

    public async Task<IEnumerable<Owner>> GetOwnersByAnimalIdAsync(int animalId)
    {
        return await _dbSet
            .Where(o => o.AnimalOwners.Any(ao => ao.AnimalId == animalId))
            .ToListAsync();
    }

    public async Task<Owner?> GetOwnerWithAnimalsAsync(int id)
    {
        return await _dbSet
            .Include(o => o.AnimalOwners)
                .ThenInclude(ao => ao.Animal)
            .FirstOrDefaultAsync(o => o.Id == id);
    }
}
