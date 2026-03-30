using AgroLink.Application.Common.Exceptions;
using AgroLink.Application.Features.OwnerBrands.DTOs;
using AgroLink.Application.Interfaces;
using AgroLink.Domain.Interfaces;
using MediatR;

namespace AgroLink.Application.Features.AnimalBrands.Queries.GetSuggestions;

public record GetAnimalBrandSuggestionsQuery(int FarmId, int AnimalId)
    : IRequest<IEnumerable<OwnerBrandDto>>;

public class GetAnimalBrandSuggestionsQueryHandler(
    IAnimalRepository animalRepository,
    IAnimalOwnerRepository animalOwnerRepository,
    IOwnerBrandRepository ownerBrandRepository,
    IStorageService storageService
) : IRequestHandler<GetAnimalBrandSuggestionsQuery, IEnumerable<OwnerBrandDto>>
{
    public async Task<IEnumerable<OwnerBrandDto>> Handle(
        GetAnimalBrandSuggestionsQuery request,
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

        var animalOwners = await animalOwnerRepository.GetByAnimalIdAsync(request.AnimalId);
        var ownerIds = animalOwners.Select(ao => ao.OwnerId).ToHashSet();

        if (ownerIds.Count == 0)
        {
            return [];
        }

        var ownerBrands = await ownerBrandRepository.FindAsync(
            ob => ownerIds.Contains(ob.OwnerId) && ob.IsActive,
            cancellationToken
        );

        return ownerBrands.Select(ob => ob.ToDto(storageService));
    }
}
