using AgroLink.Core.Entities;

namespace AgroLink.Core.Interfaces;

public interface IAnimalOwnerRepository : IRepository<AnimalOwner>
{
    Task<IEnumerable<AnimalOwner>> GetByAnimalIdAsync(int animalId);
    Task<IEnumerable<AnimalOwner>> GetByOwnerIdAsync(int ownerId);
    Task RemoveByAnimalIdAsync(int animalId);
}