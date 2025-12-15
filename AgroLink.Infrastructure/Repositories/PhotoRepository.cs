using AgroLink.Application.Interfaces;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using AgroLink.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AgroLink.Infrastructure.Repositories;

public class PhotoRepository : Repository<Photo>, IPhotoRepository
{
    public PhotoRepository(AgroLinkDbContext context)
        : base(context) { }

    public async Task<IEnumerable<Photo>> GetByEntityAsync(string entityType, int entityId)
    {
        return await _dbSet
            .Where(p => p.EntityType == entityType && p.EntityId == entityId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Photo>> GetPendingUploadsAsync()
    {
        return await _dbSet.Where(p => !p.Uploaded).OrderBy(p => p.CreatedAt).ToListAsync();
    }

    public async Task<IEnumerable<Photo>> GetByEntityTypeAsync(string entityType)
    {
        return await _dbSet
            .Where(p => p.EntityType == entityType)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }
}
