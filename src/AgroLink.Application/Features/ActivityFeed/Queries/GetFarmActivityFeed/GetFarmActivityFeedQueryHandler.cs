using AgroLink.Application.Features.ActivityFeed.DTOs;
using AgroLink.Application.Interfaces;
using MediatR;
using static AgroLink.Application.Features.ActivityFeed.DTOs.ActivityFeedEventType;

namespace AgroLink.Application.Features.ActivityFeed.Queries.GetFarmActivityFeed;

public class GetFarmActivityFeedQueryHandler(IFarmActivityFeedRepository repository)
    : IRequestHandler<GetFarmActivityFeedQuery, IEnumerable<ActivityFeedItemDto>>
{
    public async Task<IEnumerable<ActivityFeedItemDto>> Handle(
        GetFarmActivityFeedQuery request,
        CancellationToken cancellationToken
    )
    {
        var farmId = request.FarmId;
        var limit = request.Limit;

        var movements = await repository.GetFarmMovementsAsync(farmId, limit, cancellationToken);
        var notes = await repository.GetFarmNotesAsync(farmId, limit, cancellationToken);
        var retirements = await repository.GetFarmRetirementsAsync(
            farmId,
            limit,
            cancellationToken
        );
        var newborns = await repository.GetFarmNewbornsAsync(farmId, limit, cancellationToken);

        var events = new List<ActivityFeedItemDto>();

        events.AddRange(
            movements.Select(m => new ActivityFeedItemDto
            {
                EventType = Movement,
                AnimalId = m.AnimalId,
                AnimalName = string.IsNullOrEmpty(m.Animal.Name) ? null : m.Animal.Name,
                ToLotName = m.ToLot?.Name,
                OccurredAt = m.At,
            })
        );

        events.AddRange(
            notes.Select(n => new ActivityFeedItemDto
            {
                EventType = TimelineNote,
                AnimalId = n.AnimalId,
                AnimalName = string.IsNullOrEmpty(n.Animal.Name) ? null : n.Animal.Name,
                NoteContent = n.Content,
                OccurredAt = n.CreatedAt,
            })
        );

        events.AddRange(
            retirements.Select(r => new ActivityFeedItemDto
            {
                EventType = Retirement,
                AnimalId = r.AnimalId,
                AnimalName = string.IsNullOrEmpty(r.Animal.Name) ? null : r.Animal.Name,
                RetirementReason = r.Reason.ToString(),
                OccurredAt = r.At,
            })
        );

        events.AddRange(
            newborns.Select(a => new ActivityFeedItemDto
            {
                EventType = NewbornRegistration,
                AnimalId = a.Id,
                AnimalName = string.IsNullOrEmpty(a.Name) ? null : a.Name,
                OccurredAt = a.BirthDate,
            })
        );

        return events.OrderByDescending(e => e.OccurredAt).Take(limit);
    }
}
