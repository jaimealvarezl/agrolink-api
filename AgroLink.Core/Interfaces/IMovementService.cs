using AgroLink.Core.DTOs;

namespace AgroLink.Core.Interfaces;

public interface IMovementService
{
    Task<IEnumerable<MovementDto>> GetByEntityAsync(string entityType, int entityId);
    Task<MovementDto> CreateAsync(CreateMovementDto dto, int userId);
    Task<IEnumerable<MovementDto>> GetAnimalHistoryAsync(int animalId);
}
