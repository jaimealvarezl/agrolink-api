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
        return await _dbSet.Where(p => p.AnimalId == animalId).ToListAsync();
    }

    public async Task SetProfilePhotoAsync(int animalId, int photoId)
    {
        var currentProfiles = await _dbSet
            .Where(p => p.AnimalId == animalId && p.IsProfile)
            .ToListAsync();

        foreach (var photo in currentProfiles)
        {
            photo.IsProfile = false;
        }

        var newProfile = await _dbSet.FindAsync(photoId);
        newProfile?.IsProfile = true;
    }

    public async Task<bool> HasPhotosAsync(int animalId)
    {
        return await _dbSet.AnyAsync(p => p.AnimalId == animalId);
    }
}
