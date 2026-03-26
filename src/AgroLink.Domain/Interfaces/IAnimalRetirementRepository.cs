using AgroLink.Domain.Entities;

namespace AgroLink.Domain.Interfaces;

public interface IAnimalRetirementRepository
{
    Task<AnimalRetirement?> GetByAnimalIdAsync(int animalId);
    Task AddAsync(AnimalRetirement retirement);
}
