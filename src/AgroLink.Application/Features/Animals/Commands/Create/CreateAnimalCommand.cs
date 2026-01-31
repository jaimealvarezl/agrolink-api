using AgroLink.Application.Features.Animals.DTOs;
using AgroLink.Application.Features.Photos.DTOs;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Enums;
using AgroLink.Domain.Interfaces;
using MediatR;

namespace AgroLink.Application.Features.Animals.Commands.Create;

public record CreateAnimalCommand(CreateAnimalDto Dto) : IRequest<AnimalDto>;

public class CreateAnimalCommandHandler(
    IAnimalRepository animalRepository,
    ILotRepository lotRepository,
    IOwnerRepository ownerRepository,
    IAnimalOwnerRepository animalOwnerRepository,
    IUnitOfWork unitOfWork
) : IRequestHandler<CreateAnimalCommand, AnimalDto>
{
    public async Task<AnimalDto> Handle(
        CreateAnimalCommand request,
        CancellationToken cancellationToken
    )
    {
        var dto = request.Dto;
        var animal = new Animal
        {
            Cuia = dto.Cuia,
            TagVisual = dto.TagVisual,
            Name = dto.Name,
            Color = dto.Color,
            Breed = dto.Breed,
            Sex = dto.Sex,
            LifeStatus = ParseEnumOrDefault(dto.LifeStatus, LifeStatus.Active, nameof(LifeStatus)),
            ProductionStatus = ParseEnumOrDefault(
                dto.ProductionStatus,
                ProductionStatus.Calf,
                nameof(ProductionStatus)
            ),
            HealthStatus = ParseEnumOrDefault(
                dto.HealthStatus,
                HealthStatus.Healthy,
                nameof(HealthStatus)
            ),
            ReproductiveStatus = ParseEnumOrDefault(
                dto.ReproductiveStatus,
                ReproductiveStatus.NotApplicable,
                nameof(ReproductiveStatus)
            ),
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

        // Map to DTO (duplicated logic from Query for independence)
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

        // Newly created animal won't have photos yet, so empty list
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

    private static T ParseEnumOrDefault<T>(string? value, T defaultValue, string propertyName)
        where T : struct, Enum
    {
        if (string.IsNullOrEmpty(value))
        {
            return defaultValue;
        }

        if (Enum.TryParse<T>(value, true, out var result))
        {
            return result;
        }

        throw new ArgumentException(
            $"Invalid {propertyName}: {value}. Allowed values: {string.Join(", ", Enum.GetNames<T>())}"
        );
    }
}
