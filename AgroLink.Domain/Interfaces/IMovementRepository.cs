using AgroLink.Domain.Entities;

namespace AgroLink.Domain.Interfaces;

public interface IMovementRepository : IRepository<Movement>
{
    Task<IEnumerable<Movement>> GetByEntityAsync(string entityType, int entityId);
    Task<IEnumerable<Movement>> GetAnimalHistoryAsync(int animalId);
    Task<IEnumerable<Movement>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);
}
