using AgroLink.Application.Common.Exceptions;
using AgroLink.Application.Common.Utilities;
using AgroLink.Application.Features.Animals.DTOs;
using AgroLink.Application.Features.Photos.DTOs;
using AgroLink.Application.Interfaces;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using MediatR;

namespace AgroLink.Application.Features.Animals.Commands.Create;

public record CreateAnimalCommand(CreateAnimalDto Dto) : IRequest<AnimalDto>;

public class CreateAnimalCommandHandler(
    IAnimalRepository animalRepository,
    ILotRepository lotRepository,
    IOwnerRepository ownerRepository,
    IAnimalOwnerRepository animalOwnerRepository,
    IFarmMemberRepository farmMemberRepository,
    ICurrentUserService currentUserService,
    IUnitOfWork unitOfWork
) : IRequestHandler<CreateAnimalCommand, AnimalDto>
{
    public async Task<AnimalDto> Handle(
        CreateAnimalCommand request,
        CancellationToken cancellationToken
    )
    {
        var dto = request.Dto;

        // 1. Ensure Lot exists and get FarmId
        var lot = await lotRepository.GetLotWithPaddockAsync(dto.LotId);
        if (lot == null)
        {
            throw new ArgumentException($"Lot with ID {dto.LotId} not found.");
        }

        var farmId = lot.Paddock.FarmId;

        // 2. Ensure user has permissions on the Farm
        var userId = currentUserService.GetRequiredUserId();
        var isMember = await farmMemberRepository.ExistsAsync(fm =>
            fm.FarmId == farmId && fm.UserId == userId
        );
        if (!isMember)
        {
            throw new ForbiddenAccessException("User does not have permission for this Farm.");
        }

        // 3. Validate Status Consistency
        AnimalValidator.ValidateStatusConsistency(
            dto.Sex,
            dto.ProductionStatus,
            dto.ReproductiveStatus
        );

        // 4. Ensure CUIA (if provided) is unique within the Farm
        if (!string.IsNullOrEmpty(dto.Cuia))
        {
            var isUnique = await animalRepository.IsCuiaUniqueInFarmAsync(dto.Cuia, farmId);
            if (!isUnique)
            {
                throw new ArgumentException($"CUIA '{dto.Cuia}' already exists in this Farm.");
            }
        }

        // 5. Ensure Name is unique within the Farm
        var isNameUnique = await animalRepository.IsNameUniqueInFarmAsync(dto.Name, farmId);
        if (!isNameUnique)
        {
            throw new ArgumentException(
                $"Animal with name '{dto.Name}' already exists in this Farm."
            );
        }

        // 6. Ensure at least one owner is provided
        if (dto.Owners == null || dto.Owners.Count == 0)
        {
            throw new ArgumentException("At least one owner is required for an animal.");
        }

        var animal = new Animal
        {
            Cuia = dto.Cuia,
            TagVisual = dto.TagVisual,
            Name = dto.Name,
            Color = dto.Color,
            Breed = dto.Breed,
            Sex = dto.Sex,
            LifeStatus = dto.LifeStatus,
            ProductionStatus = dto.ProductionStatus,
            HealthStatus = dto.HealthStatus,
            ReproductiveStatus = dto.ReproductiveStatus,
            BirthDate = dto.BirthDate,
            LotId = dto.LotId,
            MotherId = dto.MotherId,
            FatherId = dto.FatherId,
        };

        await animalRepository.AddAsync(animal);
        await unitOfWork.SaveChangesAsync();

        // Add owners
        foreach (var ownerDto in dto.Owners)
        {
            var animalOwner = new AnimalOwner
            {
                AnimalId = animal.Id,
                OwnerId = ownerDto.OwnerId,
                SharePercent = ownerDto.SharePercent,
            };
            await animalOwnerRepository.AddAsync(animalOwner);
        }

        await unitOfWork.SaveChangesAsync();

        // Map to DTO
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

        var photoDtos = new List<PhotoDto>();

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
            LotName = lot.Name,
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
