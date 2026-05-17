using AgroLink.Application.Features.ActivityFeed.DTOs;
using MediatR;

namespace AgroLink.Application.Features.ActivityFeed.Queries.GetFarmActivityFeed;

public record GetFarmActivityFeedQuery(int FarmId, int Limit)
    : IRequest<IEnumerable<ActivityFeedItemDto>>;
