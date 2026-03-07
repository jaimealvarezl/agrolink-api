using AgroLink.Domain.Entities;

namespace AgroLink.Application.Interfaces;

public interface IMovementRepository
{
    Task<IEnumerable<Movement>> GetMovementsByEntityAsync(string entityType, int entityId);
    Task AddMovementAsync(Movement movement);
}
