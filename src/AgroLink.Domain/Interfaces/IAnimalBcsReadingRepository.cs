using AgroLink.Domain.Entities;

namespace AgroLink.Domain.Interfaces;

public interface IAnimalBcsReadingRepository
{
    Task AddAsync(AnimalBcsReading reading, CancellationToken cancellationToken = default);
}
