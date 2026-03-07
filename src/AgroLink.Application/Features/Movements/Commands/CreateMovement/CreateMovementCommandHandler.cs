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

        // Pre-fetch global data to avoid N+1 queries during DTO mapping
        var user = await movementRepository.GetUserByIdAsync(request.UserId);
        var userName = user?.Name ?? "";
        var toLotName = toLot.Name;

        var movementDataList =
            new List<(Movement Movement, string? AnimalName, string? FromLotName)>();

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

                // Capture current lot as FromId and its name
                int? fromId = animal.LotId;
                string? fromLotName = currentLot.Name;

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

                // Store entity objects/strings in memory for creating the DTO later
                // without re-querying the database
                movementDataList.Add((movement, animal.TagVisual, fromLotName));
            }

            await unitOfWork.SaveChangesAsync();
            await unitOfWork.CommitTransactionAsync();
        }
        catch
        {
            await unitOfWork.RollbackTransactionAsync();
            throw;
        }

        // Map DTOs purely in memory now that EF has populated Movement Ids
        var dtos = movementDataList
            .Select(data => new MovementDto
            {
                Id = data.Movement.Id,
                EntityType = data.Movement.EntityType,
                EntityId = data.Movement.EntityId,
                EntityName = data.AnimalName,
                FromId = data.Movement.FromId,
                FromName = data.FromLotName,
                ToId = data.Movement.ToId,
                ToName = toLotName,
                At = data.Movement.At,
                Reason = data.Movement.Reason,
                UserId = data.Movement.UserId,
                UserName = userName,
                CreatedAt = data.Movement.CreatedAt,
            })
            .ToList();

        return dtos;
    }
}
