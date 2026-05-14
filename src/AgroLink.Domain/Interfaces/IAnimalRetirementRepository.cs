using AgroLink.Domain.Entities;

namespace AgroLink.Domain.Interfaces;

public interface IAnimalRetirementRepository
{
    Task<AnimalRetirement?> GetByAnimalIdAsync(
        int animalId,
        CancellationToken cancellationToken = default
    );

    Task AddAsync(AnimalRetirement retirement, CancellationToken cancellationToken);
}
