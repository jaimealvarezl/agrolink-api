using AgroLink.Core.Entities;

namespace AgroLink.Core.Interfaces;

public interface IMovementRepository : IRepository<Movement>
{
    Task<IEnumerable<Movement>> GetByEntityAsync(string entityType, int entityId);
    Task<IEnumerable<Movement>> GetAnimalHistoryAsync(int animalId);
    Task<IEnumerable<Movement>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);
}