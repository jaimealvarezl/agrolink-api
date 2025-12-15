using AgroLink.Domain.Entities;

namespace AgroLink.Domain.Interfaces;

public interface IAnimalOwnerRepository : IRepository<AnimalOwner>
{
    Task<IEnumerable<AnimalOwner>> GetByAnimalIdAsync(int animalId);
    Task<IEnumerable<AnimalOwner>> GetByOwnerIdAsync(int ownerId);
    Task RemoveByAnimalIdAsync(int animalId);
}
