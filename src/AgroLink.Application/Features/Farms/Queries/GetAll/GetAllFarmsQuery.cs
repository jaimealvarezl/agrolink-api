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
        var memberships = await farmMemberRepository.FindAsync(m => m.UserId == request.UserId);
        var farmIds = memberships.Select(m => m.FarmId).ToList();

        var farms = await farmRepository.FindAsync(f => farmIds.Contains(f.Id));

        return farms.Select(f =>
        {
            var role = memberships.FirstOrDefault(m => m.FarmId == f.Id)?.Role ?? string.Empty;
            return new FarmDto
            {
                Id = f.Id,
                Name = f.Name,
                Location = f.Location,
                CUE = f.CUE,
                OwnerId = f.OwnerId,
                Role = role,
                CreatedAt = f.CreatedAt,
            };
        });
    }
}
