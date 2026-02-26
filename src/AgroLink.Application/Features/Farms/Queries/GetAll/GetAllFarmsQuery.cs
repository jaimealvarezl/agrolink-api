using AgroLink.Application.Features.Farms.DTOs;
using AgroLink.Domain.Interfaces;
using MediatR;

namespace AgroLink.Application.Features.Farms.Queries.GetAll;

public record GetAllFarmsQuery(int UserId) : IRequest<IEnumerable<FarmDto>>;

public class GetAllFarmsQueryHandler(
    IFarmRepository farmRepository,
    IFarmMemberRepository farmMemberRepository
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

        // Get all farms the user has access to
        var farms = await farmRepository.FindAsync(f => memberFarmIds.Contains(f.Id));

        return farms.Select(f =>
        {
            var role = memberships.FirstOrDefault(m => m.FarmId == f.Id)?.Role;

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
