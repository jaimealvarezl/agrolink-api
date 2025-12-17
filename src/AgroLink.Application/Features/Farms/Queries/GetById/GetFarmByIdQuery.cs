using AgroLink.Application.Features.Farms.DTOs;
using AgroLink.Domain.Interfaces;
using MediatR;

namespace AgroLink.Application.Features.Farms.Queries.GetById;

public record GetFarmByIdQuery(int Id) : IRequest<FarmDto?>;

public class GetFarmByIdQueryHandler(IFarmRepository farmRepository)
    : IRequestHandler<GetFarmByIdQuery, FarmDto?>
{
    public async Task<FarmDto?> Handle(
        GetFarmByIdQuery request,
        CancellationToken cancellationToken
    )
    {
        var farm = await farmRepository.GetByIdAsync(request.Id);
        if (farm == null)
        {
            return null;
        }

        return new FarmDto
        {
            Id = farm.Id,
            Name = farm.Name,
            Location = farm.Location,
            CreatedAt = farm.CreatedAt,
        };
    }
}
