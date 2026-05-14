using AgroLink.Domain.Entities;

namespace AgroLink.Application.Interfaces;

public interface IMovementRepository
{
    Task<IEnumerable<Movement>> GetMovementsByAnimalAsync(
        int animalId,
        CancellationToken cancellationToken = default
    );

    Task AddMovementAsync(Movement movement, CancellationToken cancellationToken = default);
}
