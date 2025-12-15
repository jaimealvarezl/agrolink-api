using AgroLink.Application.DTOs;
using AgroLink.Application.Interfaces;
using AgroLink.Core.Entities;
using AgroLink.Core.Interfaces;

namespace AgroLink.Infrastructure.Services;

public class FarmService(IFarmRepository farmRepository) : IFarmService
{
    public async Task<FarmDto?> GetByIdAsync(int id)
    {
        var farm = await farmRepository.GetByIdAsync(id);
        if (farm == null)
        {
            return null;
        }

        return new FarmDto
        {
            Id = farm.Id,
            Name = farm.Name,
            Location = farm.Location,
            CreatedAt = farm.CreatedAt,
        };
    }

    public async Task<IEnumerable<FarmDto>> GetAllAsync()
    {
        var farms = await farmRepository.GetAllAsync();
        return farms.Select(f => new FarmDto
        {
            Id = f.Id,
            Name = f.Name,
            Location = f.Location,
            CreatedAt = f.CreatedAt,
        });
    }

    public async Task<FarmDto> CreateAsync(CreateFarmDto dto)
    {
        var farm = new Farm { Name = dto.Name, Location = dto.Location };

        await farmRepository.AddAsync(farm);

        return new FarmDto
        {
            Id = farm.Id,
            Name = farm.Name,
            Location = farm.Location,
            CreatedAt = farm.CreatedAt,
        };
    }

    public async Task<FarmDto> UpdateAsync(int id, UpdateFarmDto dto)
    {
        var farm = await farmRepository.GetByIdAsync(id);
        if (farm == null)
        {
            throw new ArgumentException("Farm not found");
        }

        if (!string.IsNullOrEmpty(dto.Name))
        {
            farm.Name = dto.Name;
        }

        if (dto.Location != null)
        {
            farm.Location = dto.Location;
        }

        farm.UpdatedAt = DateTime.UtcNow;

        farmRepository.Update(farm);

        return new FarmDto
        {
            Id = farm.Id,
            Name = farm.Name,
            Location = farm.Location,
            CreatedAt = farm.CreatedAt,
        };
    }

    public async Task DeleteAsync(int id)
    {
        var farm = await farmRepository.GetByIdAsync(id);
        if (farm == null)
        {
            throw new ArgumentException("Farm not found");
        }

        farmRepository.Remove(farm);
    }
}
