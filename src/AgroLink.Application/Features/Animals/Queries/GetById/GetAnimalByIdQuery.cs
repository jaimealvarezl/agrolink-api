using AgroLink.Application.Common.Exceptions;
using AgroLink.Application.Features.Animals.DTOs;
using AgroLink.Application.Interfaces;
using AgroLink.Domain.Interfaces;
using MediatR;

namespace AgroLink.Application.Features.Animals.Queries.GetById;

public record GetAnimalByIdQuery(int Id) : IRequest<AnimalDto?>;

public class GetAnimalByIdQueryHandler(
    IAnimalRepository animalRepository,
    ILotRepository lotRepository,
    IFarmMemberRepository farmMemberRepository,
    ICurrentUserService currentUserService,
    IOwnerRepository ownerRepository,
    IAnimalOwnerRepository animalOwnerRepository,
    IAnimalPhotoRepository animalPhotoRepository,
    IStorageService storageService
) : IRequestHandler<GetAnimalByIdQuery, AnimalDto?>
{
    public async Task<AnimalDto?> Handle(
        GetAnimalByIdQuery request,
        CancellationToken cancellationToken
    )
    {
        var animal = await animalRepository.GetByIdAsync(request.Id);
        if (animal == null)
        {
            return null;
        }

        var lot = await lotRepository.GetLotWithPaddockAsync(animal.LotId);
        if (lot == null)
        {
            return null;
        }

        var userId = currentUserService.GetRequiredUserId();
        var isMember = await farmMemberRepository.ExistsAsync(fm =>
            fm.UserId == userId && fm.FarmId == lot.Paddock.FarmId
        );

        if (!isMember)
        {
            throw new ForbiddenAccessException("You do not have access to this animal.");
        }

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
