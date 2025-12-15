using AgroLink.Application.DTOs;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using MediatR;

namespace AgroLink.Application.Features.Animals.Commands.Create;

public record CreateAnimalCommand(CreateAnimalDto Dto) : IRequest<AnimalDto>;

public class CreateAnimalCommandHandler(
    IAnimalRepository animalRepository,
    ILotRepository lotRepository,
    IOwnerRepository ownerRepository,
    IAnimalOwnerRepository animalOwnerRepository
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
            Tag = dto.Tag,
            Name = dto.Name,
            Color = dto.Color,
            Breed = dto.Breed,
            Sex = dto.Sex,
            BirthDate = dto.BirthDate,
            LotId = dto.LotId,
            MotherId = dto.MotherId,
            FatherId = dto.FatherId,
        };

        await animalRepository.AddAsync(animal);
        await animalRepository.SaveChangesAsync();

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

        await animalRepository.SaveChangesAsync();

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
