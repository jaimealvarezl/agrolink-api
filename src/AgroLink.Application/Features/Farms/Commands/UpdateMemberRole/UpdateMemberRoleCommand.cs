using AgroLink.Application.Common.Exceptions;
using AgroLink.Application.Features.Farms.DTOs;
using AgroLink.Domain.Constants;
using AgroLink.Domain.Interfaces;
using MediatR;

namespace AgroLink.Application.Features.Farms.Commands.UpdateMemberRole;

public record UpdateMemberRoleCommand(int FarmId, int UserId, string NewRole, int CurrentUserId)
    : IRequest<FarmMemberDto>;

public class UpdateMemberRoleCommandHandler(
    IFarmMemberRepository farmMemberRepository,
    IUnitOfWork unitOfWork
) : IRequestHandler<UpdateMemberRoleCommand, FarmMemberDto>
{
    public async Task<FarmMemberDto> Handle(
        UpdateMemberRoleCommand request,
        CancellationToken cancellationToken
    )
    {
        var member =
            await farmMemberRepository.GetByFarmAndUserAsync(request.FarmId, request.UserId, true)
            ?? throw new NotFoundException("FarmMember", $"{request.FarmId}-{request.UserId}");

        // Security Check: Prevent the Owner from downgrading their own role
        if (
            request.UserId == request.CurrentUserId
            && member.Role == FarmMemberRoles.Owner
            && request.NewRole != FarmMemberRoles.Owner
        )
        {
            throw new ArgumentException("Owners cannot downgrade their own role.");
        }

        member.Role = request.NewRole;
        farmMemberRepository.Update(member);
        await unitOfWork.SaveChangesAsync();

        return new FarmMemberDto
        {
            UserId = member.UserId,
            Name = member.User?.Name ?? string.Empty,
            Email = member.User?.Email ?? string.Empty,
            Role = member.Role,
            JoinedAt = member.JoinedAt,
        };
    }
}
