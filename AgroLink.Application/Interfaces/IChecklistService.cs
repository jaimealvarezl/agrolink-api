using AgroLink.Application.DTOs;

namespace AgroLink.Application.Interfaces;

public interface IChecklistService
{
    Task<ChecklistDto?> GetByIdAsync(int id);
    Task<IEnumerable<ChecklistDto>> GetAllAsync();
    Task<IEnumerable<ChecklistDto>> GetByScopeAsync(string scopeType, int scopeId);
    Task<ChecklistDto> CreateAsync(CreateChecklistDto dto, int userId);
    Task<ChecklistDto> UpdateAsync(int id, CreateChecklistDto dto);
    Task DeleteAsync(int id);
}
