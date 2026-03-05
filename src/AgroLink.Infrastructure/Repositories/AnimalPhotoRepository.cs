using AgroLink.Application.Interfaces;
using AgroLink.Domain.Entities;
using AgroLink.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AgroLink.Infrastructure.Repositories;

public class AnimalPhotoRepository(AgroLinkDbContext context)
    : Repository<AnimalPhoto>(context),
        IAnimalPhotoRepository
{
    public async Task<IEnumerable<AnimalPhoto>> GetByAnimalIdAsync(int animalId)
    {
        return await _dbSet.AsNoTracking().Where(p => p.AnimalId == animalId).ToListAsync();
    }

    public async Task SetProfilePhotoAsync(int animalId, int photoId)
    {
        // Bulk update to unset existing profile photos
        await _dbSet
            .Where(p => p.AnimalId == animalId && p.IsProfile)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.IsProfile, false));

        // Set new profile photo
        var newProfile = await _dbSet.FindAsync(photoId);
        newProfile?.IsProfile = true;
    }

    public async Task<bool> HasPhotosAsync(int animalId)
    {
        return await _dbSet.AnyAsync(p => p.AnimalId == animalId);
    }
}
