using AgroLink.Application.Features.Animals.DTOs;
using AgroLink.Application.Interfaces;
using AgroLink.Domain.Interfaces;
using MediatR;

namespace AgroLink.Application.Features.Animals.Queries.GetAll;

public record GetAllAnimalsQuery : IRequest<IEnumerable<AnimalDto>>;

public class GetAllAnimalsQueryHandler(
    IAnimalRepository animalRepository,
    ILotRepository lotRepository,
    IOwnerRepository ownerRepository,
    IAnimalOwnerRepository animalOwnerRepository,
    IAnimalPhotoRepository animalPhotoRepository
) : IRequestHandler<GetAllAnimalsQuery, IEnumerable<AnimalDto>>
{
    public async Task<IEnumerable<AnimalDto>> Handle(
        GetAllAnimalsQuery request,
        CancellationToken cancellationToken
    )
    {
        var animals = await animalRepository.GetAllAsync();
        var result = new List<AnimalDto>();

        foreach (var animal in animals)
        {
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
                    UriRemote = p.UriRemote,
                    IsProfile = p.IsProfile,
                    ContentType = p.ContentType,
                    Size = p.Size,
                    Description = p.Description,
                    UploadedAt = p.UploadedAt,
                    CreatedAt = p.CreatedAt,
                })
                .ToList();

            result.Add(
                new AnimalDto
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
                }
            );
        }

        return result;
    }
}
