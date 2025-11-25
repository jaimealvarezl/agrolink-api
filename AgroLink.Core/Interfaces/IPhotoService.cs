using AgroLink.Core.DTOs;

namespace AgroLink.Core.Interfaces;

public interface IPhotoService
{
    Task<PhotoDto> UploadPhotoAsync(CreatePhotoDto dto, Stream fileStream, string fileName);
    Task<IEnumerable<PhotoDto>> GetByEntityAsync(string entityType, int entityId);
    Task DeleteAsync(int id);
    Task SyncPendingPhotosAsync();
}
