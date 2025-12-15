using AgroLink.Application.Interfaces;
using AgroLink.Domain.Entities;
using AgroLink.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AgroLink.Infrastructure.Repositories;

public class PhotoRepository(AgroLinkDbContext context) : IPhotoRepository
{
    public async Task AddPhotoAsync(Photo photo)
    {
        context.Photos.Add(photo);
        await context.SaveChangesAsync();
    }

    public async Task<IEnumerable<Photo>> GetPhotosByEntityAsync(string entityType, int entityId)
    {
        return await context
            .Photos.Where(p => p.EntityType == entityType && p.EntityId == entityId)
            .ToListAsync();
    }

    public async Task<Photo?> GetPhotoByIdAsync(int id)
    {
        return await context.Photos.FindAsync(id);
    }

    public async Task DeletePhotoAsync(Photo photo)
    {
        context.Photos.Remove(photo);
        await context.SaveChangesAsync();
    }

    public async Task UpdatePhotoAsync(Photo photo)
    {
        context.Photos.Update(photo);
        await context.SaveChangesAsync();
    }

    public async Task<IEnumerable<Photo>> GetPendingPhotosAsync()
    {
        return await context.Photos.Where(p => !p.Uploaded).ToListAsync();
    }
}
