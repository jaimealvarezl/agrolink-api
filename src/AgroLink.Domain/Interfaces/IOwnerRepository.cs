using AgroLink.Domain.Entities;

namespace AgroLink.Domain.Interfaces;

public interface IOwnerRepository : IRepository<Owner>
{
    Task<IEnumerable<Owner>> GetOwnersByAnimalIdAsync(int animalId);
    Task<Owner?> GetOwnerWithAnimalsAsync(int id);
}
