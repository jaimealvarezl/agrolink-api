using AgroLink.Application.DTOs;
using MediatR;

namespace AgroLink.Application.Features.Movements.Commands.CreateMovement;

public record CreateMovementCommand(CreateMovementDto MovementDto, int UserId)
    : IRequest<MovementDto>;
