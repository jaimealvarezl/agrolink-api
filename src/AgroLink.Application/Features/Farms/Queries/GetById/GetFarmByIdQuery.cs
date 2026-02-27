using AgroLink.Application.Features.Farms.DTOs;
using AgroLink.Domain.Interfaces;
using MediatR;

namespace AgroLink.Application.Features.Farms.Queries.GetById;

public record GetFarmByIdQuery(int Id, int UserId) : IRequest<FarmDto?>;

public class GetFarmByIdQueryHandler(
    IFarmRepository farmRepository,
    IFarmMemberRepository farmMemberRepository
) : IRequestHandler<GetFarmByIdQuery, FarmDto?>
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

        var membership = await farmMemberRepository.FirstOrDefaultAsync(m =>
            m.FarmId == request.Id && m.UserId == request.UserId
        );

        if (membership == null)
        {
            return null;
        }

        return new FarmDto
        {
            Id = farm.Id,
            Name = farm.Name,
            Location = farm.Location,
            CUE = farm.CUE,
            OwnerId = farm.OwnerId,
            Role = membership.Role,
            CreatedAt = farm.CreatedAt,
        };
    }
}
