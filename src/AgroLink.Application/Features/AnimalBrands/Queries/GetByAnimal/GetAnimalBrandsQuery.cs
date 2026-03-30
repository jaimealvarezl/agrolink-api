using AgroLink.Application.Common.Exceptions;
using AgroLink.Application.Features.AnimalBrands.DTOs;
using AgroLink.Application.Interfaces;
using AgroLink.Domain.Interfaces;
using MediatR;

namespace AgroLink.Application.Features.AnimalBrands.Queries.GetByAnimal;

public record GetAnimalBrandsQuery(int FarmId, int AnimalId)
    : IRequest<IEnumerable<AnimalBrandDto>>;

public class GetAnimalBrandsQueryHandler(
    IAnimalRepository animalRepository,
    IAnimalBrandRepository animalBrandRepository,
    IStorageService storageService
) : IRequestHandler<GetAnimalBrandsQuery, IEnumerable<AnimalBrandDto>>
{
    public async Task<IEnumerable<AnimalBrandDto>> Handle(
        GetAnimalBrandsQuery request,
        CancellationToken cancellationToken
    )
    {
        var animal = await animalRepository.GetByIdInFarmAsync(
            request.AnimalId,
            request.FarmId,
            cancellationToken
        );
        if (animal is null)
        {
            throw new NotFoundException(
                $"Animal with ID {request.AnimalId} not found in farm {request.FarmId}."
            );
        }

        var brands = await animalBrandRepository.GetByAnimalIdAsync(
            request.AnimalId,
            cancellationToken
        );
        return brands.Select(b => b.ToDto(storageService));
    }
}
