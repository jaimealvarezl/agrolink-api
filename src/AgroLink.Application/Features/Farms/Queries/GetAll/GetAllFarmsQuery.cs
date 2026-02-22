using AgroLink.Application.Features.Farms.DTOs;
using AgroLink.Domain.Constants;
using AgroLink.Domain.Interfaces;
using MediatR;

namespace AgroLink.Application.Features.Farms.Queries.GetAll;

public record GetAllFarmsQuery(int UserId) : IRequest<IEnumerable<FarmDto>>;

public class GetAllFarmsQueryHandler(
    IFarmRepository farmRepository,
    IFarmMemberRepository farmMemberRepository,
    IOwnerRepository ownerRepository
) : IRequestHandler<GetAllFarmsQuery, IEnumerable<FarmDto>>
{
    public async Task<IEnumerable<FarmDto>> Handle(
        GetAllFarmsQuery request,
        CancellationToken cancellationToken
    )
    {
        // Get all memberships
        var memberships = await farmMemberRepository.FindAsync(m => m.UserId == request.UserId);
        var memberFarmIds = memberships.Select(m => m.FarmId).ToList();

        // Also check if user is the direct owner of any farm (via Owner table)
        var ownerIds = (await ownerRepository.FindAsync(o => o.UserId == request.UserId))
            .Select(o => o.Id)
            .ToList();

        var ownedFarms = await farmRepository.FindAsync(f => ownerIds.Contains(f.OwnerId));
        var ownedFarmIds = ownedFarms.Select(f => f.Id).ToList();

        // Combine all farm IDs
        var allFarmIds = memberFarmIds.Union(ownedFarmIds).ToList();

        // Get all farms the user has access to
        var farms = await farmRepository.FindAsync(f => allFarmIds.Contains(f.Id));

        return farms.Select(f =>
        {
            // Determine role: Priority to Membership table, fallback to "Owner" if they own it
            var role = memberships.FirstOrDefault(m => m.FarmId == f.Id)?.Role;

            if (role == null && ownerIds.Contains(f.OwnerId))
            {
                role = FarmMemberRoles.Owner;
            }

            return new FarmDto
            {
                Id = f.Id,
                Name = f.Name,
                Location = f.Location,
                CUE = f.CUE,
                OwnerId = f.OwnerId,
                Role = role ?? string.Empty,
                CreatedAt = f.CreatedAt,
            };
        });
    }
}
