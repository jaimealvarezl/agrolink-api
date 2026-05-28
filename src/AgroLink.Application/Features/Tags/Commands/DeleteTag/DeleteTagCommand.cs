using AgroLink.Application.Common.Exceptions;
using AgroLink.Domain.Constants;
using AgroLink.Domain.Interfaces;
using MediatR;

namespace AgroLink.Application.Features.Tags.Commands.DeleteTag;

public record DeleteTagCommand(int Id, int UserId) : IRequest<int>;

public class DeleteTagCommandHandler(
    ITagRepository tagRepository,
    IFarmMemberRepository farmMemberRepository
) : IRequestHandler<DeleteTagCommand, int>
{
    public async Task<int> Handle(DeleteTagCommand request, CancellationToken cancellationToken)
    {
        var existingTag = await tagRepository.GetByIdAsync(request.Id, cancellationToken);
        if (existingTag == null)
        {
            throw new NotFoundException($"Tag with ID {request.Id} not found.");
        }

        var membership = await farmMemberRepository.GetByFarmAndUserAsync(
            existingTag.FarmId,
            request.UserId,
            cancellationToken: cancellationToken
        );

        if (membership == null)
        {
            throw new ForbiddenAccessException("You do not have access to this farm's tags.");
        }

        if (membership.Role != FarmMemberRoles.Owner && membership.Role != FarmMemberRoles.Admin)
        {
            throw new ForbiddenAccessException("Only owners or admins can delete tags.");
        }

        var (_, affectedAnimals) = await tagRepository.DeleteWithCascadeAsync(
            request.Id,
            cancellationToken
        );
        return affectedAnimals;
    }
}
