using AgroLink.Application.DTOs;

namespace AgroLink.Application.Interfaces;

public interface IFarmService
{
    Task<FarmDto?> GetByIdAsync(int id);
    Task<IEnumerable<FarmDto>> GetAllAsync();
    Task<FarmDto> CreateAsync(CreateFarmDto dto);
    Task<FarmDto> UpdateAsync(int id, UpdateFarmDto dto);
    Task DeleteAsync(int id);
}
