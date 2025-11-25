using AgroLink.Core.DTOs;
using AgroLink.Core.Entities;
using AgroLink.Core.Interfaces;

namespace AgroLink.Infrastructure.Services;

public class PaddockService(IPaddockRepository paddockRepository, IFarmRepository farmRepository)
    : IPaddockService
{
    public async Task<PaddockDto?> GetByIdAsync(int id)
    {
        var paddock = await paddockRepository.GetByIdAsync(id);
        if (paddock == null)
        {
            return null;
        }

        var farm = await farmRepository.GetByIdAsync(paddock.FarmId);

        return new PaddockDto
        {
            Id = paddock.Id,
            Name = paddock.Name,
            FarmId = paddock.FarmId,
            FarmName = farm?.Name ?? "",
            CreatedAt = paddock.CreatedAt,
        };
    }

    public async Task<IEnumerable<PaddockDto>> GetAllAsync()
    {
        var paddocks = await paddockRepository.GetAllAsync();
        var result = new List<PaddockDto>();

        foreach (var paddock in paddocks)
        {
            var farm = await farmRepository.GetByIdAsync(paddock.FarmId);
            result.Add(
                new PaddockDto
                {
                    Id = paddock.Id,
                    Name = paddock.Name,
                    FarmId = paddock.FarmId,
                    FarmName = farm?.Name ?? "",
                    CreatedAt = paddock.CreatedAt,
                }
            );
        }

        return result;
    }

    public async Task<IEnumerable<PaddockDto>> GetByFarmAsync(int farmId)
    {
        var paddocks = await paddockRepository.GetByFarmIdAsync(farmId);
        var farm = await farmRepository.GetByIdAsync(farmId);

        return paddocks.Select(p => new PaddockDto
        {
            Id = p.Id,
            Name = p.Name,
            FarmId = p.FarmId,
            FarmName = farm?.Name ?? "",
            CreatedAt = p.CreatedAt,
        });
    }

    public async Task<PaddockDto> CreateAsync(CreatePaddockDto dto)
    {
        var paddock = new Paddock { Name = dto.Name, FarmId = dto.FarmId };

        await paddockRepository.AddAsync(paddock);

        var farm = await farmRepository.GetByIdAsync(paddock.FarmId);

        return new PaddockDto
        {
            Id = paddock.Id,
            Name = paddock.Name,
            FarmId = paddock.FarmId,
            FarmName = farm?.Name ?? "",
            CreatedAt = paddock.CreatedAt,
        };
    }

    public async Task<PaddockDto> UpdateAsync(int id, UpdatePaddockDto dto)
    {
        var paddock = await paddockRepository.GetByIdAsync(id);
        if (paddock == null)
        {
            throw new ArgumentException("Paddock not found");
        }

        if (!string.IsNullOrEmpty(dto.Name))
        {
            paddock.Name = dto.Name;
        }

        if (dto.FarmId.HasValue)
        {
            paddock.FarmId = dto.FarmId.Value;
        }

        paddock.UpdatedAt = DateTime.UtcNow;

        paddockRepository.Update(paddock);

        var farm = await farmRepository.GetByIdAsync(paddock.FarmId);

        return new PaddockDto
        {
            Id = paddock.Id,
            Name = paddock.Name,
            FarmId = paddock.FarmId,
            FarmName = farm?.Name ?? "",
            CreatedAt = paddock.CreatedAt,
        };
    }

    public async Task DeleteAsync(int id)
    {
        var paddock = await paddockRepository.GetByIdAsync(id);
        if (paddock == null)
        {
            throw new ArgumentException("Paddock not found");
        }

        paddockRepository.Remove(paddock);
    }
}
