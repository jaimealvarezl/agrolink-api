using AgroLink.Application.Features.Farms.DTOs;
using AgroLink.Domain.Interfaces;
using MediatR;

namespace AgroLink.Application.Features.Farms.Queries.GetFarmHierarchy;

public record GetFarmHierarchyQuery(int Id) : IRequest<FarmHierarchyDto?>;

public class GetFarmHierarchyQueryHandler(IFarmRepository farmRepository)
    : IRequestHandler<GetFarmHierarchyQuery, FarmHierarchyDto?>
{
    public async Task<FarmHierarchyDto?> Handle(
        GetFarmHierarchyQuery request,
        CancellationToken cancellationToken
    )
    {
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
                            HeadCount = l.Animals.Count,
                        })
                        .ToList(),
                })
                .ToList(),
        };
    }
}
