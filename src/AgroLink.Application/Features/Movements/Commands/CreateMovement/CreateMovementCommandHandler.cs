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
    IUserRepository userRepository,
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

        var animalIds = request.MovementDto.AnimalIds;
        if (animalIds == null || !animalIds.Any())
        {
            throw new ArgumentException("AnimalIds cannot be empty");
        }

        // Pre-fetch the destination lot
        var toLot = await lotRepository.GetLotWithPaddockAsync(request.MovementDto.ToLotId);
        if (toLot == null || toLot.Paddock.FarmId != farmId)
        {
            throw new ArgumentException("Invalid destination lot or access denied.");
        }

        // Pre-fetch the user
        var user = await userRepository.GetByIdAsync(request.UserId);
        var userName = user?.Name ?? string.Empty;

        // Fetch all requested animals in a single query
        var animals = await animalRepository.FindAsync(a => animalIds.Contains(a.Id));
        var animalsList = animals.ToList();

        if (animalsList.Count != animalIds.Distinct().Count())
        {
            throw new ArgumentException("One or more animals were not found.");
        }

        // Extract unique source lot IDs and fetch them to validate access
        var uniqueLotIds = animalsList.Select(a => a.LotId).Distinct().ToList();
        var sourceLots = await lotRepository.GetLotsWithPaddockAsync(uniqueLotIds);

        var sourceLotsDict = sourceLots.ToDictionary(l => l.Id);
        if (
            sourceLotsDict.Count != uniqueLotIds.Count
            || sourceLotsDict.Values.Any(l => l.Paddock.FarmId != farmId)
        )
        {
            throw new ForbiddenAccessException("You do not have access to some source lots");
        }

        var movementDataList =
            new List<(Movement Movement, string? AnimalName, string? FromLotName)>();

        await unitOfWork.BeginTransactionAsync();

        try
        {
            foreach (var animal in animalsList)
            {
                // Capture current lot as FromId and its name
                int? fromId = animal.LotId;
                var fromLotName = sourceLotsDict[animal.LotId].Name;

                // Update animal's lot
                animal.LotId = request.MovementDto.ToLotId;
                animalRepository.Update(animal);

                // Create movement record
                var movement = new Movement
                {
                    AnimalId = animal.Id,
                    FromLotId = fromId,
                    ToLotId = request.MovementDto.ToLotId,
                    At = request.MovementDto.At,
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
                AnimalId = data.Movement.AnimalId,
                AnimalName = data.AnimalName,
                FromLotId = data.Movement.FromLotId,
                FromLotName = data.FromLotName,
                ToLotId = data.Movement.ToLotId,
                ToLotName = toLot.Name,
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
