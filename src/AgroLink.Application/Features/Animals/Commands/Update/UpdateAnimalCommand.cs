using AgroLink.Application.Common.Exceptions;
using AgroLink.Application.Common.Utilities;
using AgroLink.Application.Features.Animals.DTOs;
using AgroLink.Application.Interfaces;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Enums;
using AgroLink.Domain.Interfaces;
using MediatR;

namespace AgroLink.Application.Features.Animals.Commands.Update;

public record UpdateAnimalCommand(int Id, UpdateAnimalDto Dto, int UserId) : IRequest<AnimalDto>;

public class UpdateAnimalCommandHandler(
    IAnimalRepository animalRepository,
    ILotRepository lotRepository,
    IOwnerRepository ownerRepository,
    IAnimalOwnerRepository animalOwnerRepository,
    IAnimalPhotoRepository animalPhotoRepository,
    IFarmMemberRepository farmMemberRepository,
    IStorageService storageService,
    IUnitOfWork unitOfWork
) : IRequestHandler<UpdateAnimalCommand, AnimalDto>
{
    public async Task<AnimalDto> Handle(
        UpdateAnimalCommand request,
        CancellationToken cancellationToken
    )
    {
        var animal = await animalRepository.GetByIdAsync(request.Id, request.UserId);
        if (animal == null)
        {
            throw new ArgumentException("Animal not found or access denied.");
        }

        var farmId = animal.Lot.Paddock.FarmId;
        var dto = request.Dto;

        // If lot is changing, validate the new lot belongs to the same farm or user has access to it
        if (dto.LotId.HasValue && dto.LotId.Value != animal.LotId)
        {
            var newLot = await lotRepository.GetLotWithPaddockAsync(dto.LotId.Value);
            if (newLot == null)
            {
                throw new ArgumentException($"Lot with ID {dto.LotId.Value} not found.");
            }

            if (newLot.Paddock.FarmId != farmId)
            {
                // Verify access to the new farm if it's different
                var isMemberNewFarm = await farmMemberRepository.ExistsAsync(fm =>
                    fm.FarmId == newLot.Paddock.FarmId && fm.UserId == request.UserId
                );
                if (!isMemberNewFarm)
                {
                    throw new ForbiddenAccessException(
                        "User does not have permission for the target Farm."
                    );
                }

                farmId = newLot.Paddock.FarmId; // Update farmId context for subsequent validations
            }

            animal.LotId = dto.LotId.Value;
        }

        // Validate CUIA uniqueness if changing
        if (!string.IsNullOrEmpty(dto.Cuia) && dto.Cuia != animal.Cuia)
        {
            var isUnique = await animalRepository.IsCuiaUniqueInFarmAsync(
                dto.Cuia,
                farmId,
                animal.Id
            );
            if (!isUnique)
            {
                throw new ArgumentException($"CUIA '{dto.Cuia}' already exists in this Farm.");
            }

            animal.Cuia = dto.Cuia;
        }

        var oldStatus = animal.LifeStatus;
        var oldName = animal.Name;

        animal.Name = dto.Name ?? animal.Name;
        animal.TagVisual = dto.TagVisual ?? animal.TagVisual;
        animal.Color = dto.Color ?? animal.Color;
        animal.Breed = dto.Breed ?? animal.Breed;
        animal.Sex = dto.Sex ?? animal.Sex;

        animal.LifeStatus = dto.LifeStatus ?? animal.LifeStatus;
        animal.ProductionStatus = dto.ProductionStatus ?? animal.ProductionStatus;
        animal.HealthStatus = dto.HealthStatus ?? animal.HealthStatus;
        animal.ReproductiveStatus = dto.ReproductiveStatus ?? animal.ReproductiveStatus;

        // Validate Name uniqueness if name changed OR if animal became active/missing
        var activeStatuses = new[] { LifeStatus.Active, LifeStatus.Missing };
        var nameChanged = dto.Name != null && dto.Name != oldName;
        var becameActive =
            !activeStatuses.Contains(oldStatus) && activeStatuses.Contains(animal.LifeStatus);

        if (nameChanged || becameActive)
        {
            var isUnique = await animalRepository.IsNameUniqueInFarmAsync(
                animal.Name,
                farmId,
                animal.Id
            );
            if (!isUnique)
            {
                throw new ArgumentException(
                    $"Animal with name '{animal.Name}' already exists in this Farm."
                );
            }
        }

        // Validate consistency of the final state
        AnimalValidator.ValidateStatusConsistency(
            animal.Sex,
            animal.ProductionStatus,
            animal.ReproductiveStatus
        );

        if (dto.BirthDate.HasValue && dto.BirthDate.Value > DateTime.UtcNow)
        {
            throw new ArgumentException("Birth date cannot be in the future.");
        }

        animal.BirthDate = dto.BirthDate ?? animal.BirthDate;

        if (dto.MotherId.HasValue && dto.MotherId.Value != animal.MotherId)
        {
            var motherEntity = await animalRepository.GetByIdAsync(
                dto.MotherId.Value,
                request.UserId
            );
            if (motherEntity == null)
            {
                throw new ArgumentException(
                    $"Mother with ID {dto.MotherId.Value} not found or access denied."
                );
            }

            AnimalValidator.ValidateParentage(motherEntity, null, farmId);
            animal.MotherId = dto.MotherId.Value;
        }

        if (dto.FatherId.HasValue && dto.FatherId.Value != animal.FatherId)
        {
            var fatherEntity = await animalRepository.GetByIdAsync(
                dto.FatherId.Value,
                request.UserId
            );
            if (fatherEntity == null)
            {
                throw new ArgumentException(
                    $"Father with ID {dto.FatherId.Value} not found or access denied."
                );
            }

            AnimalValidator.ValidateParentage(null, fatherEntity, farmId);
            animal.FatherId = dto.FatherId.Value;
        }

        animal.UpdatedAt = DateTime.UtcNow;

        animalRepository.Update(animal);

        // Update owners if provided
        if (dto.Owners != null)
        {
            if (dto.Owners.Count == 0)
            {
                throw new ArgumentException("At least one owner is required for an animal.");
            }

            AnimalValidator.ValidateOwners(dto.Owners.Select(o => o.SharePercent));

            await animalOwnerRepository.RemoveByAnimalIdAsync(request.Id);

            foreach (var ownerDto in dto.Owners)
            {
                var animalOwner = new AnimalOwner
                {
                    AnimalId = request.Id,
                    OwnerId = ownerDto.OwnerId,
                    SharePercent = ownerDto.SharePercent,
                };
                await animalOwnerRepository.AddAsync(animalOwner);
            }
        }

        await unitOfWork.SaveChangesAsync();

        // Refresh data for the response
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
