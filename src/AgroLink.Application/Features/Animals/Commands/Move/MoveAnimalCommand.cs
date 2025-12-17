using AgroLink.Application.DTOs;
using AgroLink.Application.Interfaces;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using MediatR;

// Assuming this is needed

namespace AgroLink.Application.Features.Animals.Commands.Move;

public record MoveAnimalCommand(
    int AnimalId,
    int FromLotId,
    int ToLotId,
    string? Reason,
    int UserId
) : IRequest<AnimalDto>;

public class MoveAnimalCommandHandler(
    IAnimalRepository animalRepository,
    ILotRepository lotRepository,
    IOwnerRepository ownerRepository,
    IAnimalOwnerRepository animalOwnerRepository,
    IMovementRepository movementRepository, // Assuming movement repository is needed for moving animals
    IPhotoRepository photoRepository,
    IUnitOfWork unitOfWork
) : IRequestHandler<MoveAnimalCommand, AnimalDto>
{
    public async Task<AnimalDto> Handle(
        MoveAnimalCommand request,
        CancellationToken cancellationToken
    )
    {
        var animal = await animalRepository.GetByIdAsync(request.AnimalId); // Changed from request.Id
        if (animal == null)
        {
            throw new ArgumentException("Animal not found");
        }

        var oldLotId = animal.LotId;
        animal.LotId = request.ToLotId; // Changed from request.NewLotId
        animal.UpdatedAt = DateTime.UtcNow;

        animalRepository.Update(animal);

        // Record movement
        var movement = new Movement
        {
            EntityType = "ANIMAL",
            EntityId = animal.Id,
            FromId = oldLotId,
            ToId = request.ToLotId,
            At = DateTime.UtcNow,
            Reason = request.Reason,
            UserId = request.UserId, // Using request.UserId
        };
        await movementRepository.AddMovementAsync(movement);

        await unitOfWork.SaveChangesAsync();

        // Re-fetch related data for DTO (similar to GetAllAnimalsQueryHandler)
        var lot = await lotRepository.GetByIdAsync(animal.LotId);
        var mother = animal.MotherId.HasValue
            ? await animalRepository.GetByIdAsync(animal.MotherId.Value)
            : null;
        var father = animal.FatherId.HasValue
            ? await animalRepository.GetByIdAsync(animal.FatherId.Value)
            : null;

        var owners = await animalOwnerRepository.GetByAnimalIdAsync(animal.Id);
        var ownerDtos = new List<AnimalOwnerDto>();

        foreach (var owner in owners)
        {
            var ownerEntity = await ownerRepository.GetByIdAsync(owner.OwnerId);
            if (ownerEntity != null)
            {
                ownerDtos.Add(
                    new AnimalOwnerDto
                    {
                        OwnerId = owner.OwnerId,
                        OwnerName = ownerEntity.Name,
                        SharePercent = owner.SharePercent,
                    }
                );
            }
        }

        var photos = await photoRepository.GetPhotosByEntityAsync("ANIMAL", animal.Id); // Corrected method name
        var photoDtos = photos
            .Select(p => new PhotoDto
            {
                Id = p.Id,
                EntityType = p.EntityType,
                EntityId = p.EntityId,
                UriLocal = p.UriLocal,
                UriRemote = p.UriRemote,
                Uploaded = p.Uploaded,
                Description = p.Description,
                CreatedAt = p.CreatedAt,
            })
            .ToList();

        return new AnimalDto
        {
            Id = animal.Id,
            Tag = animal.Tag,
            Name = animal.Name,
            Color = animal.Color,
            Breed = animal.Breed,
            Sex = animal.Sex,
            Status = animal.Status,
            BirthDate = animal.BirthDate,
            LotId = animal.LotId,
            LotName = lot?.Name,
            MotherId = animal.MotherId,
            MotherTag = mother?.Tag,
            FatherId = animal.FatherId,
            FatherTag = father?.Tag,
            Owners = ownerDtos,
            Photos = photoDtos,
            CreatedAt = animal.CreatedAt,
            UpdatedAt = animal.UpdatedAt,
        };
    }
}
