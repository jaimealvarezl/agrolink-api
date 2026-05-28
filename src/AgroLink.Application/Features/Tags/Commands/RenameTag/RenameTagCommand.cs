using AgroLink.Application.Common.Exceptions;
using AgroLink.Application.Common.Utilities;
using AgroLink.Application.Features.Tags.DTOs;
using AgroLink.Domain.Interfaces;
using MediatR;

namespace AgroLink.Application.Features.Tags.Commands.RenameTag;

public record RenameTagCommand(int Id, int FarmId, string DisplayName, int UserId)
    : IRequest<TagDto>;

public class RenameTagCommandHandler(ITagRepository tagRepository)
    : IRequestHandler<RenameTagCommand, TagDto>
{
    public async Task<TagDto> Handle(RenameTagCommand request, CancellationToken cancellationToken)
    {
        var normalized = TagNormalizer.Normalize(request.DisplayName);

        var existingTag = await tagRepository.GetByIdAsync(request.Id, cancellationToken);
        if (existingTag == null || existingTag.FarmId != request.FarmId)
        {
            throw new NotFoundException($"Tag with ID {request.Id} not found.");
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
