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
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService
) : IRequestHandler<CreateMovementCommand, IEnumerable<MovementDto>>
{
    public async Task<IEnumerable<MovementDto>> Handle(
        CreateMovementCommand request,
        CancellationToken cancellationToken
    )
    {
        var farmId =
            currentUserService.CurrentFarmId
            ?? throw new UnauthorizedAccessException("Farm context is missing");

        if (request.MovementDto.AnimalIds == null || !request.MovementDto.AnimalIds.Any())
        {
            throw new ArgumentException("AnimalIds cannot be empty");
        }

        // Validate ToLotId
        var toLot = await lotRepository.GetLotWithPaddockAsync(request.MovementDto.ToLotId);
        if (toLot == null || toLot.Paddock.FarmId != farmId)
        {
            throw new ArgumentException("Invalid destination lot or access denied.");
        }

        var movements = new List<Movement>();

        await unitOfWork.BeginTransactionAsync();

        try
        {
            foreach (var animalId in request.MovementDto.AnimalIds)
            {
                var animal = await animalRepository.GetByIdAsync(animalId, request.UserId);
                if (animal == null)
                {
                    throw new ArgumentException($"Animal {animalId} not found.");
                }

                // Check access
                var currentLot = await lotRepository.GetLotWithPaddockAsync(animal.LotId);
                if (currentLot == null || currentLot.Paddock.FarmId != farmId)
                {
                    throw new ForbiddenAccessException(
                        $"You do not have access to animal {animalId}"
                    );
                }

                // Capture current lot as FromId
                int? fromId = animal.LotId;

                // Update animal's lot
                animal.LotId = request.MovementDto.ToLotId;
                animalRepository.Update(animal);

                // Create movement record
                var movement = new Movement
                {
                    EntityType = "ANIMAL",
                    EntityId = animal.Id,
                    FromId = fromId,
                    ToId = request.MovementDto.ToLotId,
                    At = request.MovementDto.Date,
                    Reason = request.MovementDto.Reason,
                    UserId = request.UserId,
                };

                await movementRepository.AddMovementAsync(movement);
                movements.Add(movement);
            }

            await unitOfWork.SaveChangesAsync();
            await unitOfWork.CommitTransactionAsync();
        }
        catch
        {
            await unitOfWork.RollbackTransactionAsync();
            throw;
        }

        var dtos = new List<MovementDto>();
        foreach (var m in movements)
        {
            dtos.Add(await MapToDtoAsync(m));
        }

        return dtos;
    }

    private async Task<MovementDto> MapToDtoAsync(Movement movement)
    {
        var user = await movementRepository.GetUserByIdAsync(movement.UserId);

        string? entityName = null;
        string? fromName = null;
        string? toName = null;

        var animal = await movementRepository.GetAnimalByIdAsync(movement.EntityId);
        entityName = animal?.TagVisual;

        if (movement.FromId.HasValue)
        {
            var lot = await movementRepository.GetLotByIdAsync(movement.FromId.Value);
            fromName = lot?.Name;
        }

        if (movement.ToId.HasValue)
        {
            var lot = await movementRepository.GetLotByIdAsync(movement.ToId.Value);
            toName = lot?.Name;
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
