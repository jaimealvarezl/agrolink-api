using AgroLink.Application.Features.Farms.DTOs;
using AgroLink.Domain.Constants;
using AgroLink.Domain.Interfaces;
using MediatR;

namespace AgroLink.Application.Features.Farms.Queries.GetById;

public record GetFarmByIdQuery(int Id, int UserId) : IRequest<FarmDto?>;

public class GetFarmByIdQueryHandler(
    IFarmRepository farmRepository,
    IFarmMemberRepository farmMemberRepository,
    IOwnerRepository ownerRepository
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

        var role = membership?.Role;

        if (role == null)
        {
            // Check if user is the owner via Owner table
            var owner = await ownerRepository.FirstOrDefaultAsync(o =>
                o.Id == farm.OwnerId && o.UserId == request.UserId
            );
            if (owner != null)
            {
                role = FarmMemberRoles.Owner;
            }
        }

        if (role == null)
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
            Role = role,
            CreatedAt = farm.CreatedAt,
        };
    }
}
