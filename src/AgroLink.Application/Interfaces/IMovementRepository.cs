using AgroLink.Domain.Entities;

namespace AgroLink.Application.Interfaces;

public interface IMovementRepository
{
    Task<IEnumerable<Movement>> GetMovementsByEntityAsync(string entityType, int entityId);
    Task AddMovementAsync(Movement movement);
    Task<User?> GetUserByIdAsync(int userId); // Needed for mapping DTO
    Task<Animal?> GetAnimalByIdAsync(int animalId); // Needed for mapping DTO
    Task<Lot?> GetLotByIdAsync(int lotId); // Needed for mapping DTO
    Task<Paddock?> GetPaddockByIdAsync(int paddockId); // Needed for mapping DTO
}
