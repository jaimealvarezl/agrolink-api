using AgroLink.Core.DTOs;

namespace AgroLink.Core.Interfaces;

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
