using AgroLink.Application.Features.Farms.DTOs;
using AgroLink.Domain.Interfaces;
using MediatR;

namespace AgroLink.Application.Features.Farms.Queries.GetFarmHierarchy;

public record GetFarmHierarchyQuery(int Id, int UserId) : IRequest<FarmHierarchyDto?>;

public class GetFarmHierarchyQueryHandler(
    IFarmRepository farmRepository,
    IFarmMemberRepository farmMemberRepository,
    IOwnerRepository ownerRepository
) : IRequestHandler<GetFarmHierarchyQuery, FarmHierarchyDto?>
{
    public async Task<FarmHierarchyDto?> Handle(
        GetFarmHierarchyQuery request,
        CancellationToken cancellationToken
    )
    {
        var hasAccess = await farmMemberRepository.ExistsAsync(fm =>
            fm.FarmId == request.Id && fm.UserId == request.UserId
        );

        if (!hasAccess)
        {
            // Fallback check: is user the owner of this farm?
            var farmRecord = await farmRepository.GetByIdAsync(request.Id);
            if (farmRecord != null)
            {
                hasAccess = await ownerRepository.ExistsAsync(o =>
                    o.Id == farmRecord.OwnerId && o.UserId == request.UserId
                );
            }
        }

        if (!hasAccess)
        {
            return null;
        }

        var farm = await farmRepository.GetFarmHierarchyAsync(request.Id);
        if (farm == null)
        {
            return null;
        }

        return new FarmHierarchyDto
        {
            Id = farm.Id,
            Name = farm.Name,
            Paddocks = farm
                .Paddocks.Select(p => new PaddockHierarchyDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Lots = p
                        .Lots.Select(l => new LotHierarchyDto
                        {
                            Id = l.Id,
                            Name = l.Name,
                            AnimalCount = l.AnimalCount,
                        })
                        .ToList(),
                })
                .ToList(),
        };
    }
}
