using AgroLink.Application.Common.Exceptions;
using AgroLink.Application.Features.Tags.DTOs;
using AgroLink.Domain.Constants;
using AgroLink.Domain.Interfaces;
using MediatR;

namespace AgroLink.Application.Features.Tags.Commands.UpdateTagColor;

public record UpdateTagColorCommand(int Id, string? ColorToken, int UserId) : IRequest<TagDto>;

public class UpdateTagColorCommandHandler(
    ITagRepository tagRepository,
    IFarmMemberRepository farmMemberRepository
) : IRequestHandler<UpdateTagColorCommand, TagDto>
{
    public async Task<TagDto> Handle(
        UpdateTagColorCommand request,
        CancellationToken cancellationToken
    )
    {
        if (request.ColorToken?.Length > 32)
        {
            throw new ArgumentException("Color token cannot exceed 32 characters.");
        }

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
            throw new ForbiddenAccessException("Only owners or admins can update tag colors.");
        }

        var updatedTag = await tagRepository.UpdateColorAsync(
            request.Id,
            request.ColorToken,
            cancellationToken
        );
        if (updatedTag == null)
        {
            throw new NotFoundException($"Tag with ID {request.Id} not found.");
        }

        return new TagDto
        {
            Id = updatedTag.Id,
            DisplayName = updatedTag.DisplayName,
            UsageCount = updatedTag.AnimalTags.Count,
            ColorToken = updatedTag.ColorToken,
        };
    }
}
