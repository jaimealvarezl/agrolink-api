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
