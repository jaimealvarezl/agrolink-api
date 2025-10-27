using AgroLink.Core.DTOs;

namespace AgroLink.Core.Interfaces;

public interface IPaddockService
{
    Task<PaddockDto?> GetByIdAsync(int id);
    Task<IEnumerable<PaddockDto>> GetAllAsync();
    Task<IEnumerable<PaddockDto>> GetByFarmAsync(int farmId);
    Task<PaddockDto> CreateAsync(CreatePaddockDto dto);
    Task<PaddockDto> UpdateAsync(int id, UpdatePaddockDto dto);
    Task DeleteAsync(int id);
}
