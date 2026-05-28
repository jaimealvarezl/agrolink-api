using AgroLink.Application.Common.Exceptions;
using AgroLink.Application.Common.Utilities;
using AgroLink.Application.Features.Animals.DTOs;
using AgroLink.Application.Features.Animals.Validators;
using AgroLink.Application.Interfaces;
using AgroLink.Domain.Constants;
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
    ITagRepository tagRepository,
    ICurrentUserService currentUserService,
    IOwnershipValidator ownershipValidator,
    IUnitOfWork unitOfWork
) : IRequestHandler<CreateAnimalCommand, AnimalDto>
{
    public async Task<AnimalDto> Handle(
        CreateAnimalCommand request,
        CancellationToken cancellationToken
    )
    {
        var dto = request.Dto;

        var lot = await lotRepository.GetLotWithPaddockAsync(dto.LotId, cancellationToken);
        if (lot == null)
        {
            throw new ArgumentException($"Lot with ID {dto.LotId} not found.");
        }

        var farmId = lot.Paddock.FarmId;

        // Security check: ensure target lot belongs to the current farm context
        if (
            currentUserService.CurrentFarmId.HasValue
            && farmId != currentUserService.CurrentFarmId.Value
        )
        {
            throw new ForbiddenAccessException(
                "You do not have access to create animals in this farm context."
            );
        }

        var userId = currentUserService.GetRequiredUserId();
        var membership = await farmMemberRepository.GetByFarmAndUserAsync(
            farmId,
            userId,
            cancellationToken: cancellationToken
        );
        if (membership == null)
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
            mother = await animalRepository.GetByIdAsync(
                dto.MotherId.Value,
                userId,
                cancellationToken
            );
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
            father = await animalRepository.GetByIdAsync(
                dto.FatherId.Value,
                userId,
                cancellationToken
            );
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
            var isUnique = await animalRepository.IsCuiaUniqueInFarmAsync(
                dto.Cuia,
                farmId,
                cancellationToken: cancellationToken
            );
            if (!isUnique)
            {
                throw new ArgumentException($"CUIA '{dto.Cuia}' already exists in this Farm.");
            }
        }

        var isNameUnique = await animalRepository.IsNameUniqueInFarmAsync(
            dto.Name,
            farmId,
            cancellationToken: cancellationToken
        );
        if (!isNameUnique)
        {
            throw new ArgumentException(
                $"Animal with name '{dto.Name}' already exists in this Farm."
            );
        }

        if (dto.Owners.Count == 0)
        {
            var farm =
                await farmRepository.GetByIdAsync(farmId, cancellationToken)
                ?? throw new ArgumentException($"Farm with ID {farmId} not found.");
            dto.Owners.Add(new AnimalOwnerCreateDto { OwnerId = farm.OwnerId, SharePercent = 100 });
        }

        await ownershipValidator.ValidateAsync(dto.Owners, farmId, cancellationToken);

        var normalizedTags = TagNormalizer.NormalizeDistinct(dto.Tags);
        var tagsByCanonical = new Dictionary<string, Tag>();

        if (normalizedTags.Count > 0)
        {
            var existingTags = await tagRepository.GetByCanonicalNamesAsync(
                farmId,
                normalizedTags.Select(t => t.CanonicalName),
                cancellationToken
            );
            tagsByCanonical = existingTags.ToDictionary(t => t.CanonicalName, t => t);

            if (
                membership.Role == FarmMemberRoles.Editor
                && normalizedTags.Any(t => !tagsByCanonical.ContainsKey(t.CanonicalName))
            )
            {
                throw new ForbiddenAccessException("Foreman cannot create new tags");
            }

            foreach (
                var normalizedTag in normalizedTags.Where(normalizedTag =>
                    !tagsByCanonical.ContainsKey(normalizedTag.CanonicalName)
                )
            )
            {
                var upsertedTag = await tagRepository.UpsertAsync(
                    farmId,
                    normalizedTag.DisplayName,
                    userId,
                    cancellationToken
                );
                tagsByCanonical[upsertedTag.CanonicalName] = upsertedTag;
            }
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

        foreach (
            var tag in normalizedTags.Select(normalizedTag =>
                tagsByCanonical[normalizedTag.CanonicalName]
            )
        )
        {
            animal.AnimalTags.Add(
                new AnimalTag
                {
                    TagId = tag.Id,
                    AddedByUserId = userId,
                    AddedAt = DateTime.UtcNow,
                }
            );
        }

        await animalRepository.AddAsync(animal, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var ownerDtos = new List<AnimalOwnerDto>();

        foreach (var owner in animal.AnimalOwners)
        {
            var ownerEntity = await ownerRepository.GetByIdAsync(owner.OwnerId, cancellationToken);
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
            Tags = normalizedTags.Select(t => t.DisplayName).ToList(),
            CreatedAt = animal.CreatedAt,
            UpdatedAt = animal.UpdatedAt,
        };
    }
}
