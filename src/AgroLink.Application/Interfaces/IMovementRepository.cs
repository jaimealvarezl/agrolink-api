using AgroLink.Domain.Entities;

namespace AgroLink.Application.Interfaces;

public interface IMovementRepository
{
    Task<IEnumerable<Movement>> GetMovementsByAnimalAsync(int animalId);
    Task AddMovementAsync(Movement movement);
}
