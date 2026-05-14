using AgroLink.Domain.Entities;

namespace AgroLink.Domain.Interfaces;

public interface IAnimalOwnerRepository : IRepository<AnimalOwner>
{
    Task<IEnumerable<AnimalOwner>> GetByAnimalIdAsync(
        int animalId,
        CancellationToken cancellationToken = default
    );

    Task RemoveByAnimalIdAsync(int animalId, CancellationToken cancellationToken = default);
}
