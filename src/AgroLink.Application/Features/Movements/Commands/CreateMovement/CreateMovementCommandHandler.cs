using AgroLink.Application.Common.Exceptions;
using AgroLink.Application.Features.Movements.DTOs;
using AgroLink.Application.Interfaces;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using MediatR;

namespace AgroLink.Application.Features.Movements.Commands.CreateMovement;

public class CreateMovementCommandHandler(
    IMovementRepository movementRepository,
    IAnimalRepository animalRepository,
    ILotRepository lotRepository,
    IPaddockRepository paddockRepository,
    ICurrentUserService currentUserService
) : IRequestHandler<CreateMovementCommand, MovementDto>
{
    public async Task<MovementDto> Handle(
        CreateMovementCommand request,
        CancellationToken cancellationToken
    )
    {
        // Security check: ensure all entities belong to the current farm context
        if (currentUserService.CurrentFarmId.HasValue)
        {
            var farmId = currentUserService.CurrentFarmId.Value;

            // Validate EntityId
            if (request.MovementDto.EntityType == "ANIMAL")
            {
                var animal = await animalRepository.GetByIdAsync(request.MovementDto.EntityId);
                if (animal == null) throw new ArgumentException("Animal not found");
                var lot = await lotRepository.GetLotWithPaddockAsync(animal.LotId);
                if (lot == null || lot.Paddock.FarmId != farmId)
                    throw new ForbiddenAccessException("You do not have access to this animal");
            }
            else if (request.MovementDto.EntityType == "LOT")
            {
                var lot = await lotRepository.GetLotWithPaddockAsync(request.MovementDto.EntityId);
                if (lot == null) throw new ArgumentException("Lot not found");
                if (lot.Paddock.FarmId != farmId)
                    throw new ForbiddenAccessException("You do not have access to this lot");
            }

            // Validate FromId
            if (request.MovementDto.FromId.HasValue)
            {
                if (request.MovementDto.EntityType == "ANIMAL")
                {
                    var lot = await lotRepository.GetLotWithPaddockAsync(request.MovementDto.FromId.Value);
                    if (lot != null && lot.Paddock.FarmId != farmId)
                        throw new ForbiddenAccessException("Source lot does not belong to this farm");
                }
                else if (request.MovementDto.EntityType == "LOT")
                {
                    var paddock = await paddockRepository.GetByIdAsync(request.MovementDto.FromId.Value);
                    if (paddock != null && paddock.FarmId != farmId)
                        throw new ForbiddenAccessException("Source paddock does not belong to this farm");
                }
            }

            // Validate ToId
            if (request.MovementDto.ToId.HasValue)
            {
                if (request.MovementDto.EntityType == "ANIMAL")
                {
                    var lot = await lotRepository.GetLotWithPaddockAsync(request.MovementDto.ToId.Value);
                    if (lot != null && lot.Paddock.FarmId != farmId)
                        throw new ForbiddenAccessException("Target lot does not belong to this farm");
                }
                else if (request.MovementDto.EntityType == "LOT")
                {
                    var paddock = await paddockRepository.GetByIdAsync(request.MovementDto.ToId.Value);
                    if (paddock != null && paddock.FarmId != farmId)
                        throw new ForbiddenAccessException("Target paddock does not belong to this farm");
                }
            }
        }

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

        await movementRepository.AddMovementAsync(movement);

        return await MapToDtoAsync(movement);
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
