using AgroLink.Application.Features.Animals.DTOs;
using AgroLink.Domain.Interfaces;
using MediatR;

namespace AgroLink.Application.Features.Animals.Queries.GetDetail;

public class GetAnimalDetailQueryHandler(IAnimalRepository animalRepository)
    : IRequestHandler<GetAnimalDetailQuery, AnimalDetailDto?>
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

        var ageInMonths =
            (DateTime.UtcNow.Year - animal.BirthDate.Year) * 12
            + DateTime.UtcNow.Month
            - animal.BirthDate.Month;

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
            MotherName = animal.Mother?.Name ?? animal.Mother?.TagVisual,
            FatherName = animal.Father?.Name ?? animal.Father?.TagVisual,
            Owners = animal
                .AnimalOwners.Select(ao => new AnimalOwnerDto
                {
                    OwnerId = ao.OwnerId,
                    OwnerName = ao.Owner.Name,
                    SharePercent = ao.SharePercent,
                })
                .ToList(),
            PrimaryPhotoUrl = animal.Photos.FirstOrDefault()?.UriRemote
        };
    }
}
