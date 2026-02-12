using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;

namespace AgroLink.Application.Interfaces;

public interface IAnimalPhotoRepository : IRepository<AnimalPhoto>
{
    Task<IEnumerable<AnimalPhoto>> GetByAnimalIdAsync(int animalId);
    Task SetProfilePhotoAsync(int animalId, int photoId);
    Task<bool> HasPhotosAsync(int animalId);
}
