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

    public async Task<AnimalPhoto?> GetByIdAsync(int id)
    {
        return await _dbSet.FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task SetProfilePhotoAsync(int animalId, int photoId)
    {
        var photos = await _dbSet.Where(p => p.AnimalId == animalId).ToListAsync();
        foreach (var photo in photos)
        {
            photo.IsProfile = photo.Id == photoId;
        }
    }

    public async Task<bool> HasPhotosAsync(int animalId)
    {
        return await _dbSet.AnyAsync(p => p.AnimalId == animalId);
    }
}
