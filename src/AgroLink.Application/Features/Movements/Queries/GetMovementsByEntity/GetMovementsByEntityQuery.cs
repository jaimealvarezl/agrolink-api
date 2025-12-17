using AgroLink.Application.DTOs;
using MediatR;

namespace AgroLink.Application.Features.Movements.Queries.GetMovementsByEntity;

public record GetMovementsByEntityQuery(string EntityType, int EntityId)
    : IRequest<IEnumerable<MovementDto>>;
