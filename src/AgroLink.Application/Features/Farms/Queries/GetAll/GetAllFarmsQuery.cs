using AgroLink.Application.DTOs;
using AgroLink.Domain.Interfaces;
using MediatR;

namespace AgroLink.Application.Features.Farms.Queries.GetAll;

public record GetAllFarmsQuery : IRequest<IEnumerable<FarmDto>>;

public class GetAllFarmsQueryHandler(IFarmRepository farmRepository)
    : IRequestHandler<GetAllFarmsQuery, IEnumerable<FarmDto>>
{
    public async Task<IEnumerable<FarmDto>> Handle(
        GetAllFarmsQuery request,
        CancellationToken cancellationToken
    )
    {
        var farms = await farmRepository.GetAllAsync();
        return farms.Select(f => new FarmDto
        {
            Id = f.Id,
            Name = f.Name,
            Location = f.Location,
            CreatedAt = f.CreatedAt,
        });
    }
}
