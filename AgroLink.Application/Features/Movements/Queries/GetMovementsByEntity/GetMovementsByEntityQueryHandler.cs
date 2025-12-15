using AgroLink.Application.DTOs;
using AgroLink.Domain.Entities;
using AgroLink.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AgroLink.Application.Features.Movements.Queries.GetMovementsByEntity;

public class GetMovementsByEntityQueryHandler(AgroLinkDbContext context)
    : IRequestHandler<GetMovementsByEntityQuery, IEnumerable<MovementDto>>
{
    public async Task<IEnumerable<MovementDto>> Handle(
        GetMovementsByEntityQuery request,
        CancellationToken cancellationToken
    )
    {
        var movements = await context
            .Movements.Where(m =>
                m.EntityType == request.EntityType && m.EntityId == request.EntityId
            )
            .ToListAsync(cancellationToken);
        var result = new List<MovementDto>();

        foreach (var movement in movements)
        {
            result.Add(await MapToDtoAsync(movement));
        }

        return result.OrderByDescending(m => m.At);
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
