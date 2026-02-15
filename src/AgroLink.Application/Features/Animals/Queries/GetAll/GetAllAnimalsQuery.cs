using AgroLink.Application.Features.Animals.DTOs;
using AgroLink.Application.Interfaces;
using AgroLink.Domain.Interfaces;
using MediatR;

namespace AgroLink.Application.Features.Animals.Queries.GetAll;

public record GetAllAnimalsQuery(int UserId) : IRequest<IEnumerable<AnimalDto>>;

public class GetAllAnimalsQueryHandler(
    IAnimalRepository animalRepository,
    IStorageService storageService
) : IRequestHandler<GetAllAnimalsQuery, IEnumerable<AnimalDto>>
{
    public async Task<IEnumerable<AnimalDto>> Handle(
        GetAllAnimalsQuery request,
        CancellationToken cancellationToken
    )
    {
        var animals = await animalRepository.GetAllByUserAsync(request.UserId, cancellationToken);

        return animals
            .Select(animal => new AnimalDto
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
                LotName = animal.Lot?.Name,
                MotherId = animal.MotherId,
                MotherCuia = animal.Mother?.Cuia,
                FatherId = animal.FatherId,
                FatherCuia = animal.Father?.Cuia,
                Owners = animal
                    .AnimalOwners.Select(ao => new AnimalOwnerDto
                    {
                        OwnerId = ao.OwnerId,
                        OwnerName = ao.Owner?.Name ?? "Unknown",
                        SharePercent = ao.SharePercent,
                    })
                    .ToList(),
                Photos = animal
                    .Photos.Select(p => new AnimalPhotoDto
                    {
                        Id = p.Id,
                        AnimalId = p.AnimalId,
                        UriRemote = storageService.GetPresignedUrl(
                            p.StorageKey,
                            TimeSpan.FromHours(1)
                        ),
                        IsProfile = p.IsProfile,
                        ContentType = p.ContentType,
                        Size = p.Size,
                        Description = p.Description,
                        UploadedAt = p.UploadedAt,
                        CreatedAt = p.CreatedAt,
                    })
                    .ToList(),
                CreatedAt = animal.CreatedAt,
                UpdatedAt = animal.UpdatedAt,
            })
            .ToList();
    }
}
