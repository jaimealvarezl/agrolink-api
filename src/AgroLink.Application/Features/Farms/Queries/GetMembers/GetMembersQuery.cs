using AgroLink.Application.Features.Farms.DTOs;
using AgroLink.Domain.Interfaces;
using MediatR;

namespace AgroLink.Application.Features.Farms.Queries.GetMembers;

public record GetMembersQuery(int FarmId) : IRequest<IEnumerable<FarmMemberDto>>;

public class GetMembersQueryHandler(IFarmMemberRepository farmMemberRepository)
    : IRequestHandler<GetMembersQuery, IEnumerable<FarmMemberDto>>
{
    public async Task<IEnumerable<FarmMemberDto>> Handle(
        GetMembersQuery request,
        CancellationToken cancellationToken
    )
    {
        var members = await farmMemberRepository.GetByFarmIdWithUserAsync(request.FarmId);

        return members.Select(m => new FarmMemberDto
        {
            UserId = m.UserId,
            Name = m.User.Name,
            Email = m.User.Email,
            Role = m.Role,
            JoinedAt = m.JoinedAt,
        });
    }
}
