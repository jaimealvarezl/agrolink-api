using AgroLink.Core.Entities;

namespace AgroLink.Core.Interfaces;

public interface IPhotoRepository : IRepository<Photo>
{
    Task<IEnumerable<Photo>> GetByEntityAsync(string entityType, int entityId);
    Task<IEnumerable<Photo>> GetPendingUploadsAsync();
    Task<IEnumerable<Photo>> GetByEntityTypeAsync(string entityType);
}
