using AgroLink.Domain.Entities;

namespace AgroLink.Application.Interfaces;

public interface IPhotoRepository
{
    Task AddPhotoAsync(Photo photo);
    Task<IEnumerable<Photo>> GetPhotosByEntityAsync(string entityType, int entityId);
    Task<Photo?> GetPhotoByIdAsync(int id);
    Task DeletePhotoAsync(Photo photo);
    Task UpdatePhotoAsync(Photo photo);
    Task<IEnumerable<Photo>> GetPendingPhotosAsync();
}
