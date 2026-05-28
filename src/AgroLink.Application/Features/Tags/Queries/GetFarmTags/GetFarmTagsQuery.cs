using AgroLink.Application.Features.Tags.DTOs;
using AgroLink.Domain.Interfaces;
using MediatR;

namespace AgroLink.Application.Features.Tags.Queries.GetFarmTags;

public record GetFarmTagsQuery(int FarmId, string? Search = null) : IRequest<List<TagDto>>;

public class GetFarmTagsQueryHandler(ITagRepository tagRepository)
    : IRequestHandler<GetFarmTagsQuery, List<TagDto>>
{
    public async Task<List<TagDto>> Handle(
        GetFarmTagsQuery request,
        CancellationToken cancellationToken
    )
    {
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
