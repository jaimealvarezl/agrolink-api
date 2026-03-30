using AgroLink.Domain.Entities;

namespace AgroLink.Domain.Interfaces;

public interface IAnimalBrandRepository : IRepository<AnimalBrand>
{
    Task<IEnumerable<AnimalBrand>> GetByAnimalIdAsync(int animalId, CancellationToken ct = default);
}
