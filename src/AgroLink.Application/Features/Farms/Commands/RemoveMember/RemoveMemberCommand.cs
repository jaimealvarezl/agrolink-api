using AgroLink.Application.Common.Exceptions;
using AgroLink.Domain.Constants;
using AgroLink.Domain.Interfaces;
using MediatR;

namespace AgroLink.Application.Features.Farms.Commands.RemoveMember;

public record RemoveMemberCommand(int FarmId, int UserId) : IRequest;

public class RemoveMemberCommandHandler(
    IFarmMemberRepository farmMemberRepository,
    IUnitOfWork unitOfWork
) : IRequestHandler<RemoveMemberCommand>
{
    public async Task Handle(RemoveMemberCommand request, CancellationToken cancellationToken)
    {
        var member =
            await farmMemberRepository.GetByFarmAndUserAsync(request.FarmId, request.UserId)
            ?? throw new NotFoundException("FarmMember", $"{request.FarmId}-{request.UserId}");

        // Security Check: Prevent removing the last Owner of the farm
        if (member.Role == FarmMemberRoles.Owner)
        {
            var ownersCount = await farmMemberRepository.CountAsync(fm =>
                fm.FarmId == request.FarmId && fm.Role == FarmMemberRoles.Owner
            );

            if (ownersCount <= 1)
            {
                throw new ArgumentException(
                    "Cannot remove the last owner of the farm. Promote another member to owner first."
                );
            }
        }

        farmMemberRepository.Remove(member);
        await unitOfWork.SaveChangesAsync();
    }
}
