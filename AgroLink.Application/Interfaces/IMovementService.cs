using AgroLink.Application.DTOs;

namespace AgroLink.Application.Interfaces;

public interface IMovementService
{
    Task<IEnumerable<MovementDto>> GetByEntityAsync(string entityType, int entityId);
    Task<MovementDto> CreateAsync(CreateMovementDto dto, int userId);
    Task<IEnumerable<MovementDto>> GetAnimalHistoryAsync(int animalId);
}
