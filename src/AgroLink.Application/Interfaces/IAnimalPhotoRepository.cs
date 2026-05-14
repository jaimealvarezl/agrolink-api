using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;

namespace AgroLink.Application.Interfaces;

public interface IAnimalPhotoRepository : IRepository<AnimalPhoto>
{
    Task<IEnumerable<AnimalPhoto>> GetByAnimalIdAsync(
        int animalId,
        CancellationToken cancellationToken = default
    );

    Task SetProfilePhotoAsync(
        int animalId,
        int photoId,
        CancellationToken cancellationToken = default
    );

    Task<bool> HasPhotosAsync(int animalId, CancellationToken cancellationToken = default);
}
