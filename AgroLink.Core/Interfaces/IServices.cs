using AgroLink.Core.DTOs;

namespace AgroLink.Core.Interfaces;

public interface IAnimalService
{
    Task<AnimalDto?> GetByIdAsync(int id);
    Task<IEnumerable<AnimalDto>> GetAllAsync();
    Task<IEnumerable<AnimalDto>> GetByLotAsync(int lotId);
    Task<AnimalDto> CreateAsync(CreateAnimalDto dto);
    Task<AnimalDto> UpdateAsync(int id, UpdateAnimalDto dto);
    Task DeleteAsync(int id);
    Task<AnimalGenealogyDto?> GetGenealogyAsync(int id);
    Task<AnimalDto> MoveAnimalAsync(
        int animalId,
        int fromLotId,
        int toLotId,
        string? reason,
        int userId
    );
}

public interface IChecklistService
{
    Task<ChecklistDto?> GetByIdAsync(int id);
    Task<IEnumerable<ChecklistDto>> GetAllAsync();
    Task<IEnumerable<ChecklistDto>> GetByScopeAsync(string scopeType, int scopeId);
    Task<ChecklistDto> CreateAsync(CreateChecklistDto dto, int userId);
    Task<ChecklistDto> UpdateAsync(int id, CreateChecklistDto dto);
    Task DeleteAsync(int id);
}

public interface IMovementService
{
    Task<IEnumerable<MovementDto>> GetByEntityAsync(string entityType, int entityId);
    Task<MovementDto> CreateAsync(CreateMovementDto dto, int userId);
    Task<IEnumerable<MovementDto>> GetAnimalHistoryAsync(int animalId);
}

public interface IPhotoService
{
    Task<PhotoDto> UploadPhotoAsync(CreatePhotoDto dto, Stream fileStream, string fileName);
    Task<IEnumerable<PhotoDto>> GetByEntityAsync(string entityType, int entityId);
    Task DeleteAsync(int id);
    Task SyncPendingPhotosAsync();
}

public interface IAuthService
{
    Task<AuthResponseDto?> LoginAsync(LoginDto dto);
    Task<UserDto> RegisterAsync(UserDto dto, string password);
    Task<bool> ValidateTokenAsync(string token);
    Task<UserDto?> GetUserFromTokenAsync(string token);
    
    // New methods for controller logic
    Task<UserDto> RegisterUserAsync(RegisterRequest request);
    Task<UserDto?> GetUserProfileAsync(string token);
    Task<object> ValidateTokenResponseAsync(string token);
}

public interface IFarmService
{
    Task<FarmDto?> GetByIdAsync(int id);
    Task<IEnumerable<FarmDto>> GetAllAsync();
    Task<FarmDto> CreateAsync(CreateFarmDto dto);
    Task<FarmDto> UpdateAsync(int id, UpdateFarmDto dto);
    Task DeleteAsync(int id);
}

public interface ILotService
{
    Task<LotDto?> GetByIdAsync(int id);
    Task<IEnumerable<LotDto>> GetAllAsync();
    Task<IEnumerable<LotDto>> GetByPaddockAsync(int paddockId);
    Task<LotDto> CreateAsync(CreateLotDto dto);
    Task<LotDto> UpdateAsync(int id, UpdateLotDto dto);
    Task DeleteAsync(int id);
    Task<LotDto> MoveLotAsync(int lotId, int toPaddockId, string? reason, int userId);
}

public interface IPaddockService
{
    Task<PaddockDto?> GetByIdAsync(int id);
    Task<IEnumerable<PaddockDto>> GetAllAsync();
    Task<IEnumerable<PaddockDto>> GetByFarmAsync(int farmId);
    Task<PaddockDto> CreateAsync(CreatePaddockDto dto);
    Task<PaddockDto> UpdateAsync(int id, UpdatePaddockDto dto);
    Task DeleteAsync(int id);
}
