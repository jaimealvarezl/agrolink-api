using AgroLink.Core.DTOs;
using AgroLink.Core.Entities;
using AgroLink.Core.Interfaces;
using AgroLink.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AgroLink.Infrastructure.Services;

public class LotService(
    ILotRepository lotRepository,
    IPaddockRepository paddockRepository,
    IMovementRepository movementRepository
) : ILotService
{
    public async Task<LotDto?> GetByIdAsync(int id)
    {
        var lot = await lotRepository.GetByIdAsync(id);
        if (lot == null)
            return null;

        var paddock = await paddockRepository.GetByIdAsync(lot.PaddockId);

        return new LotDto
        {
            Id = lot.Id,
            Name = lot.Name,
            PaddockId = lot.PaddockId,
            PaddockName = paddock?.Name ?? "",
            Status = lot.Status,
            CreatedAt = lot.CreatedAt,
        };
    }

    public async Task<IEnumerable<LotDto>> GetAllAsync()
    {
        var lots = await lotRepository.GetAllAsync();
        var result = new List<LotDto>();

        foreach (var lot in lots)
        {
            var paddock = await paddockRepository.GetByIdAsync(lot.PaddockId);
            result.Add(
                new LotDto
                {
                    Id = lot.Id,
                    Name = lot.Name,
                    PaddockId = lot.PaddockId,
                    PaddockName = paddock?.Name ?? "",
                    Status = lot.Status,
                    CreatedAt = lot.CreatedAt,
                }
            );
        }

        return result;
    }

    public async Task<IEnumerable<LotDto>> GetByPaddockAsync(int paddockId)
    {
        var lots = await lotRepository.GetByPaddockIdAsync(paddockId);
        var paddock = await paddockRepository.GetByIdAsync(paddockId);

        return lots.Select(l => new LotDto
        {
            Id = l.Id,
            Name = l.Name,
            PaddockId = l.PaddockId,
            PaddockName = paddock?.Name ?? "",
            Status = l.Status,
            CreatedAt = l.CreatedAt,
        });
    }

    public async Task<LotDto> CreateAsync(CreateLotDto dto)
    {
        var lot = new Lot
        {
            Name = dto.Name,
            PaddockId = dto.PaddockId,
            Status = dto.Status ?? "ACTIVE",
        };

        await lotRepository.AddAsync(lot);

        var paddock = await paddockRepository.GetByIdAsync(lot.PaddockId);

        return new LotDto
        {
            Id = lot.Id,
            Name = lot.Name,
            PaddockId = lot.PaddockId,
            PaddockName = paddock?.Name ?? "",
            Status = lot.Status,
            CreatedAt = lot.CreatedAt,
        };
    }

    public async Task<LotDto> UpdateAsync(int id, UpdateLotDto dto)
    {
        var lot = await lotRepository.GetByIdAsync(id);
        if (lot == null)
            throw new ArgumentException("Lot not found");

        if (!string.IsNullOrEmpty(dto.Name))
            lot.Name = dto.Name;

        if (dto.PaddockId.HasValue)
            lot.PaddockId = dto.PaddockId.Value;

        if (!string.IsNullOrEmpty(dto.Status))
            lot.Status = dto.Status;

        lot.UpdatedAt = DateTime.UtcNow;

        lotRepository.Update(lot);

        var paddock = await paddockRepository.GetByIdAsync(lot.PaddockId);

        return new LotDto
        {
            Id = lot.Id,
            Name = lot.Name,
            PaddockId = lot.PaddockId,
            PaddockName = paddock?.Name ?? "",
            Status = lot.Status,
            CreatedAt = lot.CreatedAt,
        };
    }

    public async Task DeleteAsync(int id)
    {
        var lot = await lotRepository.GetByIdAsync(id);
        if (lot == null)
            throw new ArgumentException("Lot not found");

        lotRepository.Remove(lot);
    }

    public async Task<LotDto> MoveLotAsync(int lotId, int toPaddockId, string? reason, int userId)
    {
        var lot = await lotRepository.GetByIdAsync(lotId);
        if (lot == null)
            throw new ArgumentException("Lot not found");

        var fromPaddockId = lot.PaddockId;
        lot.PaddockId = toPaddockId;
        lot.UpdatedAt = DateTime.UtcNow;

        lotRepository.Update(lot);

        // Record movement
        var movement = new Movement
        {
            EntityType = "LOT",
            EntityId = lotId,
            FromId = fromPaddockId,
            ToId = toPaddockId,
            At = DateTime.UtcNow,
            Reason = reason,
            UserId = userId,
        };

        await movementRepository.AddAsync(movement);

        var paddock = await paddockRepository.GetByIdAsync(lot.PaddockId);

        return new LotDto
        {
            Id = lot.Id,
            Name = lot.Name,
            PaddockId = lot.PaddockId,
            PaddockName = paddock?.Name ?? "",
            Status = lot.Status,
            CreatedAt = lot.CreatedAt,
        };
    }
}
