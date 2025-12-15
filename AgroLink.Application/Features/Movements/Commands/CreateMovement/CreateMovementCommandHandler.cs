using AgroLink.Application.DTOs;
using AgroLink.Domain.Entities;
using AgroLink.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AgroLink.Application.Features.Movements.Commands.CreateMovement;

public class CreateMovementCommandHandler(AgroLinkDbContext context)
    : IRequestHandler<CreateMovementCommand, MovementDto>
{
    public async Task<MovementDto> Handle(
        CreateMovementCommand request,
        CancellationToken cancellationToken
    )
    {
        var movement = new Movement
        {
            EntityType = request.MovementDto.EntityType,
            EntityId = request.MovementDto.EntityId,
            FromId = request.MovementDto.FromId,
            ToId = request.MovementDto.ToId,
            At = request.MovementDto.At,
            Reason = request.MovementDto.Reason,
            UserId = request.UserId,
        };

        context.Movements.Add(movement);
        await context.SaveChangesAsync(cancellationToken);

        return await MapToDtoAsync(movement);
    }

    private async Task<MovementDto> MapToDtoAsync(Movement movement)
    {
        var user = await context.Users.FindAsync(movement.UserId);

        string? entityName = null;
        string? fromName = null;
        string? toName = null;

        // Get entity name
        if (movement.EntityType == "ANIMAL")
        {
            var animal = await context.Animals.FindAsync(movement.EntityId);
            entityName = animal?.Tag;
        }
        else if (movement.EntityType == "LOT")
        {
            var lot = await context.Lots.FindAsync(movement.EntityId);
            entityName = lot?.Name;
        }

        // Get from name
        if (movement.FromId.HasValue)
        {
            if (movement.EntityType == "ANIMAL")
            {
                var lot = await context.Lots.FindAsync(movement.FromId.Value);
                fromName = lot?.Name;
            }
            else if (movement.EntityType == "LOT")
            {
                var paddock = await context.Paddocks.FindAsync(movement.FromId.Value);
                fromName = paddock?.Name;
            }
        }

        // Get to name
        if (movement.ToId.HasValue)
        {
            if (movement.EntityType == "ANIMAL")
            {
                var lot = await context.Lots.FindAsync(movement.ToId.Value);
                toName = lot?.Name;
            }
            else if (movement.EntityType == "LOT")
            {
                var paddock = await context.Paddocks.FindAsync(movement.ToId.Value);
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
