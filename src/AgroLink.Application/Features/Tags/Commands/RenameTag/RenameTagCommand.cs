using AgroLink.Application.Common.Exceptions;
using AgroLink.Application.Common.Utilities;
using AgroLink.Application.Features.Tags.DTOs;
using AgroLink.Domain.Constants;
using AgroLink.Domain.Interfaces;
using MediatR;

namespace AgroLink.Application.Features.Tags.Commands.RenameTag;

public record RenameTagCommand(int Id, string DisplayName, int UserId) : IRequest<TagDto>;

public class RenameTagCommandHandler(
    ITagRepository tagRepository,
    IFarmMemberRepository farmMemberRepository
) : IRequestHandler<RenameTagCommand, TagDto>
{
    public async Task<TagDto> Handle(RenameTagCommand request, CancellationToken cancellationToken)
    {
        var normalized = TagNormalizer.Normalize(request.DisplayName);

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
            throw new ForbiddenAccessException("Only owners or admins can rename tags.");
        }

        var tag = await tagRepository.RenameAsync(
            request.Id,
            normalized.DisplayName,
            cancellationToken
        );
        if (tag == null)
        {
            throw new NotFoundException($"Tag with ID {request.Id} not found.");
        }

        return new TagDto
        {
            Id = tag.Id,
            DisplayName = tag.DisplayName,
            UsageCount = tag.AnimalTags.Count,
            ColorToken = tag.ColorToken,
        };
    }
}
