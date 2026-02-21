using AgroLink.Application.Common.Exceptions;
using AgroLink.Application.Common.Utilities;
using AgroLink.Application.Features.Animals.DTOs;
using AgroLink.Application.Interfaces;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using MediatR;

namespace AgroLink.Application.Features.Animals.Commands.Create;

public record CreateAnimalCommand(CreateAnimalDto Dto) : IRequest<AnimalDto>;

public class CreateAnimalCommandHandler(
    IAnimalRepository animalRepository,
    ILotRepository lotRepository,
    IFarmRepository farmRepository,
    IOwnerRepository ownerRepository,
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

        var lot = await lotRepository.GetLotWithPaddockAsync(dto.LotId);
        if (lot == null)
        {
            throw new ArgumentException($"Lot with ID {dto.LotId} not found.");
        }

        var farmId = lot.Paddock.FarmId;

        var userId = currentUserService.GetRequiredUserId();
        var isMember = await farmMemberRepository.ExistsAsync(fm =>
            fm.FarmId == farmId && fm.UserId == userId
        );
        if (!isMember)
        {
            throw new ForbiddenAccessException("User does not have permission for this Farm.");
        }

        if (dto.BirthDate > DateTime.UtcNow)
        {
            throw new ArgumentException("Birth date cannot be in the future.");
        }

        AnimalValidator.ValidateStatusConsistency(
            dto.Sex,
            dto.ProductionStatus,
            dto.ReproductiveStatus
        );

        Animal? mother = null;
        if (dto.MotherId.HasValue)
        {
            mother = await animalRepository.GetByIdAsync(dto.MotherId.Value, userId);
            if (mother == null)
            {
                throw new ArgumentException(
                    $"Mother with ID {dto.MotherId.Value} not found or access denied."
                );
            }
        }

        Animal? father = null;
        if (dto.FatherId.HasValue)
        {
            father = await animalRepository.GetByIdAsync(dto.FatherId.Value, userId);
            if (father == null)
            {
                throw new ArgumentException(
                    $"Father with ID {dto.FatherId.Value} not found or access denied."
                );
            }
        }

        AnimalValidator.ValidateParentage(mother, father, farmId);

        if (!string.IsNullOrEmpty(dto.Cuia))
        {
            var isUnique = await animalRepository.IsCuiaUniqueInFarmAsync(dto.Cuia, farmId);
            if (!isUnique)
            {
                throw new ArgumentException($"CUIA '{dto.Cuia}' already exists in this Farm.");
            }
        }

        var isNameUnique = await animalRepository.IsNameUniqueInFarmAsync(dto.Name, farmId);
        if (!isNameUnique)
        {
            throw new ArgumentException(
                $"Animal with name '{dto.Name}' already exists in this Farm."
            );
        }

        if (dto.Owners.Count == 0)
        {
            var farm =
                await farmRepository.GetByIdAsync(farmId)
                ?? throw new ArgumentException($"Farm with ID {farmId} not found.");
            dto.Owners.Add(new AnimalOwnerCreateDto { OwnerId = farm.OwnerId, SharePercent = 100 });
        }
        else
        {
            AnimalValidator.ValidateOwners(dto.Owners.Select(o => o.SharePercent));
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

        foreach (var ownerDto in dto.Owners)
        {
            animal.AnimalOwners.Add(
                new AnimalOwner { OwnerId = ownerDto.OwnerId, SharePercent = ownerDto.SharePercent }
            );
        }

        await animalRepository.AddAsync(animal);
        await unitOfWork.SaveChangesAsync();

        var ownerDtos = new List<AnimalOwnerDto>();

        foreach (var owner in animal.AnimalOwners)
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

        var photoDtos = new List<AnimalPhotoDto>();

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
