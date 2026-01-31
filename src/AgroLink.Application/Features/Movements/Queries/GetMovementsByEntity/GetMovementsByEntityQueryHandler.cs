using AgroLink.Application.Features.Movements.DTOs;
using AgroLink.Application.Interfaces;
using AgroLink.Domain.Entities;
using MediatR;

// For IMovementRepository

namespace AgroLink.Application.Features.Movements.Queries.GetMovementsByEntity;

public class GetMovementsByEntityQueryHandler(IMovementRepository movementRepository)
    : IRequestHandler<GetMovementsByEntityQuery, IEnumerable<MovementDto>>
{
    public async Task<IEnumerable<MovementDto>> Handle(
        GetMovementsByEntityQuery request,
        CancellationToken cancellationToken
    )
    {
        var movements = await movementRepository.GetMovementsByEntityAsync(
            request.EntityType,
            request.EntityId
        );
        var result = new List<MovementDto>();

        foreach (var movement in movements)
        {
            result.Add(await MapToDtoAsync(movement));
        }

        return result.OrderByDescending(m => m.At);
    }

    private async Task<MovementDto> MapToDtoAsync(Movement movement)
    {
        var user = await movementRepository.GetUserByIdAsync(movement.UserId);

        string? entityName = null;
        string? fromName = null;
        string? toName = null;

        // Get entity name
        if (movement.EntityType == "ANIMAL")
        {
            var animal = await movementRepository.GetAnimalByIdAsync(movement.EntityId);
            entityName = animal?.TagVisual;
        }
        else if (movement.EntityType == "LOT")
        {
            var lot = await movementRepository.GetLotByIdAsync(movement.EntityId);
            entityName = lot?.Name;
        }

        // Get from name
        if (movement.FromId.HasValue)
        {
            if (movement.EntityType == "ANIMAL")
            {
                var lot = await movementRepository.GetLotByIdAsync(movement.FromId.Value);
                fromName = lot?.Name;
            }
            else if (movement.EntityType == "LOT")
            {
                var paddock = await movementRepository.GetPaddockByIdAsync(movement.FromId.Value);
                fromName = paddock?.Name;
            }
        }

        // Get to name
        if (movement.ToId.HasValue)
        {
            if (movement.EntityType == "ANIMAL")
            {
                var lot = await movementRepository.GetLotByIdAsync(movement.ToId.Value);
                toName = lot?.Name;
            }
            else if (movement.EntityType == "LOT")
            {
                var paddock = await movementRepository.GetPaddockByIdAsync(movement.ToId.Value);
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
            CreatedAt = movement.CreatedAt,
        };
    }
}
