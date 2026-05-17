using AgroLink.Application.Features.ActivityFeed.DTOs;
using AgroLink.Application.Interfaces;
using MediatR;

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
        var events = new List<ActivityFeedItemDto>();

        var movements = await repository.GetFarmMovementsAsync(farmId, cancellationToken);
        events.AddRange(
            movements.Select(m => new ActivityFeedItemDto
            {
                EventType = "Movement",
                AnimalId = m.AnimalId,
                AnimalName = string.IsNullOrEmpty(m.Animal.Name) ? null : m.Animal.Name,
                Description =
                    m.ToLot != null ? $"Movido a {m.ToLot.Name}" : "Movimiento registrado",
                OccurredAt = m.At,
            })
        );

        var notes = await repository.GetFarmNotesAsync(farmId, cancellationToken);
        events.AddRange(
            notes.Select(n => new ActivityFeedItemDto
            {
                EventType = "TimelineNote",
                AnimalId = n.AnimalId,
                AnimalName = string.IsNullOrEmpty(n.Animal.Name) ? null : n.Animal.Name,
                Description = n.Content,
                OccurredAt = n.CreatedAt,
            })
        );

        var retirements = await repository.GetFarmRetirementsAsync(farmId, cancellationToken);
        events.AddRange(
            retirements.Select(r => new ActivityFeedItemDto
            {
                EventType = "Retirement",
                AnimalId = r.AnimalId,
                AnimalName = string.IsNullOrEmpty(r.Animal.Name) ? null : r.Animal.Name,
                Description = $"Dado de baja: {r.Reason}",
                OccurredAt = r.At,
            })
        );

        var newborns = await repository.GetFarmNewbornsAsync(farmId, cancellationToken);
        events.AddRange(
            newborns.Select(a => new ActivityFeedItemDto
            {
                EventType = "NewbornRegistration",
                AnimalId = a.Id,
                AnimalName = string.IsNullOrEmpty(a.Name) ? null : a.Name,
                Description = "Nacimiento registrado",
                OccurredAt = a.BirthDate,
            })
        );

        return events.OrderByDescending(e => e.OccurredAt).Take(request.Limit);
    }
}
