using AgroLink.Application.Features.Animals.DTOs;
using AgroLink.Application.Features.Photos.DTOs;
using AgroLink.Application.Interfaces;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Enums;
using AgroLink.Domain.Interfaces;
using MediatR;

namespace AgroLink.Application.Features.Animals.Commands.Update;

public record UpdateAnimalCommand(int Id, UpdateAnimalDto Dto) : IRequest<AnimalDto>;

public class UpdateAnimalCommandHandler(
    IAnimalRepository animalRepository,
    ILotRepository lotRepository,
    IOwnerRepository ownerRepository,
    IAnimalOwnerRepository animalOwnerRepository,
    IPhotoRepository photoRepository,
    IUnitOfWork unitOfWork
) : IRequestHandler<UpdateAnimalCommand, AnimalDto>
{
    public async Task<AnimalDto> Handle(
        UpdateAnimalCommand request,
        CancellationToken cancellationToken
    )
    {
        var animal = await animalRepository.GetByIdAsync(request.Id);
        if (animal == null)
        {
            throw new ArgumentException("Animal not found");
        }

        var dto = request.Dto;
        animal.Name = dto.Name ?? animal.Name;
        animal.TagVisual = dto.TagVisual ?? animal.TagVisual;
        animal.Color = dto.Color ?? animal.Color;
        animal.Breed = dto.Breed ?? animal.Breed;

        if (!string.IsNullOrEmpty(dto.LifeStatus))
        {
            animal.LifeStatus = ParseEnum<LifeStatus>(dto.LifeStatus, nameof(LifeStatus));
        }

        if (!string.IsNullOrEmpty(dto.ProductionStatus))
        {
            animal.ProductionStatus = ParseEnum<ProductionStatus>(
                dto.ProductionStatus,
                nameof(ProductionStatus)
            );
        }

        if (!string.IsNullOrEmpty(dto.HealthStatus))
        {
            animal.HealthStatus = ParseEnum<HealthStatus>(dto.HealthStatus, nameof(HealthStatus));
        }

        if (!string.IsNullOrEmpty(dto.ReproductiveStatus))
        {
            animal.ReproductiveStatus = ParseEnum<ReproductiveStatus>(
                dto.ReproductiveStatus,
                nameof(ReproductiveStatus)
            );
        }

        animal.BirthDate = dto.BirthDate ?? animal.BirthDate;
        animal.MotherId = dto.MotherId ?? animal.MotherId;
        animal.FatherId = dto.FatherId ?? animal.FatherId;
        animal.UpdatedAt = DateTime.UtcNow;

        animalRepository.Update(animal);

        // Update owners
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

        await unitOfWork.SaveChangesAsync();

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

        var photos = await photoRepository.GetPhotosByEntityAsync("ANIMAL", animal.Id);
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
            Cuia = animal.Cuia,
            TagVisual = animal.TagVisual,
            Name = animal.Name,
            Color = animal.Color,
            Breed = animal.Breed,
            Sex = animal.Sex,
            LifeStatus = animal.LifeStatus.ToString(),
            ProductionStatus = animal.ProductionStatus.ToString(),
            HealthStatus = animal.HealthStatus.ToString(),
            ReproductiveStatus = animal.ReproductiveStatus.ToString(),
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

    private static T ParseEnum<T>(string value, string propertyName)
        where T : struct, Enum
    {
        if (Enum.TryParse<T>(value, true, out var result))
        {
            return result;
        }

        throw new ArgumentException(
            $"Invalid {propertyName}: {value}. Allowed values: {string.Join(", ", Enum.GetNames<T>())}"
        );
    }
}
