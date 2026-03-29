using AgroLink.Application.Common.Exceptions;
using AgroLink.Application.Features.OwnerBrands.DTOs;
using AgroLink.Application.Interfaces;
using AgroLink.Domain.Interfaces;
using MediatR;

namespace AgroLink.Application.Features.OwnerBrands.Queries.GetByOwner;

public record GetOwnerBrandsQuery(int FarmId, int OwnerId) : IRequest<IEnumerable<OwnerBrandDto>>;

public class GetOwnerBrandsQueryHandler(
    IOwnerBrandRepository ownerBrandRepository,
    IOwnerRepository ownerRepository,
    IStorageService storageService
) : IRequestHandler<GetOwnerBrandsQuery, IEnumerable<OwnerBrandDto>>
{
    public async Task<IEnumerable<OwnerBrandDto>> Handle(
        GetOwnerBrandsQuery request,
        CancellationToken cancellationToken
    )
    {
        var ownerExists = await ownerRepository.ExistsAsync(o =>
            o.Id == request.OwnerId && o.FarmId == request.FarmId
        );
        if (!ownerExists)
        {
            throw new NotFoundException(
                $"Owner with ID {request.OwnerId} not found in farm {request.FarmId}."
            );
        }

        var brands = await ownerBrandRepository.FindAsync(b => b.OwnerId == request.OwnerId);

        return brands.Select(b => b.ToDto(storageService));
    }
}
