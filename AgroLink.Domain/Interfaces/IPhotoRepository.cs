using AgroLink.Domain.Entities;

namespace AgroLink.Domain.Interfaces;

public interface IPhotoRepository : IRepository<Photo>
{
    Task<IEnumerable<Photo>> GetByEntityAsync(string entityType, int entityId);
    Task<IEnumerable<Photo>> GetPendingUploadsAsync();
    Task<IEnumerable<Photo>> GetByEntityTypeAsync(string entityType);
}
