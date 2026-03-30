using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using AgroLink.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AgroLink.Infrastructure.Repositories;

public class AnimalBrandRepository(AgroLinkDbContext context)
    : Repository<AnimalBrand>(context),
        IAnimalBrandRepository
{
    public async Task<IEnumerable<AnimalBrand>> GetByAnimalIdAsync(
        int animalId,
        CancellationToken ct = default
    )
    {
        return await _dbSet
            .AsNoTracking()
            .Where(ab => ab.AnimalId == animalId)
            .Include(ab => ab.OwnerBrand)
            .ToListAsync(ct);
    }
}
