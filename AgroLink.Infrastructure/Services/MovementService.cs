using AgroLink.Core.DTOs;
using AgroLink.Core.Entities;
using AgroLink.Core.Interfaces;

namespace AgroLink.Infrastructure.Services;

public class MovementService : IMovementService
{
    private readonly IUnitOfWork _unitOfWork;

    public MovementService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<MovementDto>> GetByEntityAsync(string entityType, int entityId)
    {
        var movements = await _unitOfWork.Movements.FindAsync(m => m.EntityType == entityType && m.EntityId == entityId);
        var result = new List<MovementDto>();

        foreach (var movement in movements)
        {
            result.Add(await MapToDtoAsync(movement));
        }

        return result.OrderByDescending(m => m.At);
    }

    public async Task<MovementDto> CreateAsync(CreateMovementDto dto, int userId)
    {
        var movement = new Movement
        {
            EntityType = dto.EntityType,
            EntityId = dto.EntityId,
            FromId = dto.FromId,
            ToId = dto.ToId,
            At = dto.At,
            Reason = dto.Reason,
            UserId = userId
        };

        await _unitOfWork.Movements.AddAsync(movement);
        await _unitOfWork.SaveChangesAsync();

        return await MapToDtoAsync(movement);
    }

    public async Task<IEnumerable<MovementDto>> GetAnimalHistoryAsync(int animalId)
    {
        return await GetByEntityAsync("ANIMAL", animalId);
    }

    private async Task<MovementDto> MapToDtoAsync(Movement movement)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(movement.UserId);
        
        string? entityName = null;
        string? fromName = null;
        string? toName = null;

        // Get entity name
        if (movement.EntityType == "ANIMAL")
        {
            var animal = await _unitOfWork.Animals.GetByIdAsync(movement.EntityId);
            entityName = animal?.Tag;
        }
        else if (movement.EntityType == "LOT")
        {
            var lot = await _unitOfWork.Lots.GetByIdAsync(movement.EntityId);
            entityName = lot?.Name;
        }

        // Get from name
        if (movement.FromId.HasValue)
        {
            if (movement.EntityType == "ANIMAL")
            {
                var lot = await _unitOfWork.Lots.GetByIdAsync(movement.FromId.Value);
                fromName = lot?.Name;
            }
            else if (movement.EntityType == "LOT")
            {
                var paddock = await _unitOfWork.Paddocks.GetByIdAsync(movement.FromId.Value);
                fromName = paddock?.Name;
            }
        }

        // Get to name
        if (movement.ToId.HasValue)
        {
            if (movement.EntityType == "ANIMAL")
            {
                var lot = await _unitOfWork.Lots.GetByIdAsync(movement.ToId.Value);
                toName = lot?.Name;
            }
            else if (movement.EntityType == "LOT")
            {
                var paddock = await _unitOfWork.Paddocks.GetByIdAsync(movement.ToId.Value);
                toName = paddock?.Name;
            }
        }

        return new MovementDto
        {
            Id = movement.Id,
            EntityType = movement.EntityType,
            EntityId = movement.EntityId,
            EntityName = entityName,
            FromId = movement.FromId,
            FromName = fromName,
            ToId = movement.ToId,
            ToName = toName,
            At = movement.At,
            Reason = movement.Reason,
            UserId = movement.UserId,
            UserName = user?.Name ?? "",
            CreatedAt = movement.CreatedAt
        };
    }
}