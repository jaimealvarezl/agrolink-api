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

        var duplicate = await tagRepository.GetByCanonicalNamesAsync(
            request.FarmId,
            [normalized.CanonicalName],
            cancellationToken
        );
        if (duplicate.Any(t => t.Id != request.Id))
        {
            throw new ConflictException($"A tag named '{normalized.DisplayName}' already exists.");
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
