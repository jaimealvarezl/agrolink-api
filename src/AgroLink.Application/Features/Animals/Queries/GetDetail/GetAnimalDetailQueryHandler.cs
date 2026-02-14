using AgroLink.Application.Features.Animals.DTOs;
using AgroLink.Application.Interfaces;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using MediatR;

namespace AgroLink.Application.Features.Animals.Queries.GetDetail;

public class GetAnimalDetailQueryHandler(
    IAnimalRepository animalRepository,
    IFarmMemberRepository farmMemberRepository,
    ICurrentUserService currentUserService,
    IStorageService storageService
) : IRequestHandler<GetAnimalDetailQuery, AnimalDetailDto?>
{
    public async Task<AnimalDetailDto?> Handle(
        GetAnimalDetailQuery request,
        CancellationToken cancellationToken
    )
    {
        var animal = await animalRepository.GetAnimalDetailsAsync(request.Id);

        if (animal == null)
        {
            return null;
        }

        // IDOR check: Verify that the current user is a member of the farm that owns the animal
        var userId = currentUserService.GetRequiredUserId();
        var farmId = animal.Lot.Paddock.FarmId;
        var isMember = await farmMemberRepository.ExistsAsync(fm =>
            fm.UserId == userId && fm.FarmId == farmId
        );

        if (!isMember)
        {
            throw new UnauthorizedAccessException("You do not have access to this animal.");
        }

        var now = DateTime.UtcNow;
        var ageInMonths =
            (now.Year - animal.BirthDate.Year) * 12 + now.Month - animal.BirthDate.Month;

        if (now.Day < animal.BirthDate.Day)
        {
            ageInMonths--;
        }

        var primaryPhotoUrl = GetPrimaryPhotoUrl(animal.Photos);
        var motherPhotoUrl = GetPrimaryPhotoUrl(animal.Mother?.Photos);
        var fatherPhotoUrl = GetPrimaryPhotoUrl(animal.Father?.Photos);

        var photoDtos = animal
            .Photos.OrderByDescending(p => p.IsProfile)
            .ThenByDescending(p => p.UploadedAt)
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

        return new AnimalDetailDto
        {
            Id = animal.Id,
            TagVisual = animal.TagVisual,
            Cuia = animal.Cuia,
            Name = animal.Name,
            Color = animal.Color,
            Breed = animal.Breed,
            Sex = animal.Sex,
            BirthDate = animal.BirthDate,
            AgeInMonths = ageInMonths,
            LotId = animal.LotId,
            LotName = animal.Lot.Name,
            LifeStatus = animal.LifeStatus,
            ProductionStatus = animal.ProductionStatus,
            HealthStatus = animal.HealthStatus,
            ReproductiveStatus = animal.ReproductiveStatus,
            MotherName = animal.Mother is { } mother ? mother.Name ?? mother.TagVisual : null,
            MotherPhotoUrl = motherPhotoUrl,
            FatherName = animal.Father is { } father ? father.Name ?? father.TagVisual : null,
            FatherPhotoUrl = fatherPhotoUrl,
            Owners = animal
                .AnimalOwners.Select(ao => new AnimalOwnerDto
                {
                    OwnerId = ao.OwnerId,
                    OwnerName = ao.Owner.Name,
                    SharePercent = ao.SharePercent,
                })
                .ToList(),
            PrimaryPhotoUrl = primaryPhotoUrl,
            Photos = photoDtos,
        };
    }

    private string? GetPrimaryPhotoUrl(IEnumerable<AnimalPhoto>? photos)
    {
        if (photos == null || !photos.Any())
        {
            return null;
        }

        var primaryPhoto = photos
            .OrderByDescending(p => p.IsProfile)
            .ThenByDescending(p => p.UploadedAt)
            .FirstOrDefault();

        return primaryPhoto != null
            ? storageService.GetPresignedUrl(primaryPhoto.StorageKey, TimeSpan.FromHours(1))
            : null;
    }
}
