using AgroLink.Application.Common.Exceptions;
using AgroLink.Application.Features.Animals.DTOs;
using AgroLink.Application.Interfaces;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using MediatR;

namespace AgroLink.Application.Features.Animals.Commands.Move;

public record MoveAnimalCommand(
    int AnimalId,
    int FromLotId,
    int ToLotId,
    int UserId,
    string? Reason
) : IRequest<AnimalDto>;

public class MoveAnimalCommandHandler(
    IAnimalRepository animalRepository,
    ILotRepository lotRepository,
    IOwnerRepository ownerRepository,
    IAnimalOwnerRepository animalOwnerRepository,
    IMovementRepository movementRepository,
    IAnimalPhotoRepository animalPhotoRepository,
    IFarmMemberRepository farmMemberRepository,
    IStorageService storageService,
    IUnitOfWork unitOfWork
) : IRequestHandler<MoveAnimalCommand, AnimalDto>
{
    public async Task<AnimalDto> Handle(
        MoveAnimalCommand request,
        CancellationToken cancellationToken
    )
    {
        var animal = await animalRepository.GetByIdAsync(request.AnimalId, request.UserId);
        if (animal == null)
        {
            throw new ArgumentException("Animal not found or access denied.");
        }

        // Verify target lot and permissions
        var targetLot = await lotRepository.GetLotWithPaddockAsync(request.ToLotId);
        if (targetLot == null)
        {
            throw new ArgumentException("Target lot not found.");
        }

        var isMemberOfTargetFarm = await farmMemberRepository.ExistsAsync(fm =>
            fm.FarmId == targetLot.Paddock.FarmId && fm.UserId == request.UserId
        );

        if (!isMemberOfTargetFarm)
        {
            throw new ForbiddenAccessException("You do not have access to the target farm.");
        }

        var oldLotId = animal.LotId;
        animal.LotId = request.ToLotId;
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
            UserId = request.UserId,
        };
        await movementRepository.AddMovementAsync(movement);

        await unitOfWork.SaveChangesAsync();

        // Refresh data for response
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

        var photos = await animalPhotoRepository.GetByAnimalIdAsync(animal.Id);
        var photoDtos = photos
            .Select(p => new AnimalPhotoDto
            {
                Id = p.Id,
                AnimalId = p.AnimalId,
                UriRemote = storageService.GetPresignedUrl(p.StorageKey, TimeSpan.FromHours(1)),
                IsProfile = p.IsProfile,
                ContentType = p.ContentType,
                Size = p.Size,
                Description = p.Description,
                UploadedAt = p.UploadedAt,
                CreatedAt = p.CreatedAt,
            })
            .ToList();

        return new AnimalDto
        {
            Id = animal.Id,
            Cuia = animal.Cuia,
            TagVisual = animal.TagVisual,
            Name = animal.Name,
            Color = animal.Color,
            Breed = animal.Breed,
            Sex = animal.Sex,
            LifeStatus = animal.LifeStatus,
            ProductionStatus = animal.ProductionStatus,
            HealthStatus = animal.HealthStatus,
            ReproductiveStatus = animal.ReproductiveStatus,
            BirthDate = animal.BirthDate,
            LotId = animal.LotId,
            LotName = lot?.Name,
            MotherId = animal.MotherId,
            MotherCuia = mother?.Cuia,
            FatherId = animal.FatherId,
            FatherCuia = father?.Cuia,
            Owners = ownerDtos,
            Photos = photoDtos,
            CreatedAt = animal.CreatedAt,
            UpdatedAt = animal.UpdatedAt,
        };
    }
}
