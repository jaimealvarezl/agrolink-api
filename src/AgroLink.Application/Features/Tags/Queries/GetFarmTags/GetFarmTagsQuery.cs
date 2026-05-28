using AgroLink.Application.Common.Exceptions;
using AgroLink.Application.Features.Tags.DTOs;
using AgroLink.Application.Interfaces;
using AgroLink.Domain.Interfaces;
using MediatR;

namespace AgroLink.Application.Features.Tags.Queries.GetFarmTags;

public record GetFarmTagsQuery(int FarmId, string? Search = null) : IRequest<List<TagDto>>;

public class GetFarmTagsQueryHandler(
    ITagRepository tagRepository,
    IFarmMemberRepository farmMemberRepository,
    ICurrentUserService currentUserService
) : IRequestHandler<GetFarmTagsQuery, List<TagDto>>
{
    public async Task<List<TagDto>> Handle(
        GetFarmTagsQuery request,
        CancellationToken cancellationToken
    )
    {
        var userId = currentUserService.GetRequiredUserId();
        var isMember = await farmMemberRepository.ExistsAsync(
            fm => fm.FarmId == request.FarmId && fm.UserId == userId,
            cancellationToken
        );

        if (!isMember)
        {
            throw new ForbiddenAccessException("You do not have access to this farm's tags.");
        }

        var tags = await tagRepository.GetByFarmAsync(
            request.FarmId,
            request.Search,
            cancellationToken
        );

        return tags.Select(t => new TagDto
            {
                Id = t.Id,
                DisplayName = t.DisplayName,
                UsageCount = t.AnimalTags.Count,
                ColorToken = t.ColorToken,
            })
            .OrderByDescending(t => t.UsageCount)
            .ThenBy(t => t.DisplayName)
            .ToList();
    }
}
