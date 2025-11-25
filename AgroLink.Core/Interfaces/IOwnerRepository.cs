using AgroLink.Core.Entities;

namespace AgroLink.Core.Interfaces;

public interface IOwnerRepository : IRepository<Owner>
{
    Task<IEnumerable<Owner>> GetOwnersByAnimalIdAsync(int animalId);
    Task<Owner?> GetOwnerWithAnimalsAsync(int id);
}
