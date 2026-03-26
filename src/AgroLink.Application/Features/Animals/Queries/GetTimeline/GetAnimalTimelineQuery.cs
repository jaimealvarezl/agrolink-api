using AgroLink.Application.Common.Exceptions;
using AgroLink.Application.Features.Animals.DTOs;
using AgroLink.Application.Features.Movements.DTOs;
using AgroLink.Application.Interfaces;
using AgroLink.Domain.Interfaces;
using MediatR;

namespace AgroLink.Application.Features.Animals.Queries.GetTimeline;

public record GetAnimalTimelineQuery(int AnimalId, int FarmId)
    : IRequest<IEnumerable<AnimalTimelineItemDto>>;

public class GetAnimalTimelineQueryHandler(
    IAnimalRepository animalRepository,
    IAnimalNoteRepository animalNoteRepository,
    IAnimalRetirementRepository animalRetirementRepository,
    IMovementRepository movementRepository,
    IChecklistRepository checklistRepository,
    IUserRepository userRepository,
    ILotRepository lotRepository
) : IRequestHandler<GetAnimalTimelineQuery, IEnumerable<AnimalTimelineItemDto>>
{
    public async Task<IEnumerable<AnimalTimelineItemDto>> Handle(
        GetAnimalTimelineQuery request,
        CancellationToken cancellationToken
    )
    {
        var animal =
            await animalRepository.GetByIdInFarmAsync(request.AnimalId, request.FarmId)
            ?? throw new NotFoundException("Animal", request.AnimalId);

        var notes = await animalNoteRepository.GetByAnimalIdAsync(request.AnimalId);
        var timelineItems = notes
            .Select(note => new AnimalTimelineItemDto
            {
                Type = "note",
                OccurredAt = note.CreatedAt,
                Note = new AnimalNoteDto
                {
                    Id = note.Id,
                    AnimalId = note.AnimalId,
                    Content = note.Content,
                    UserId = note.UserId,
                    UserName = note.User?.Name ?? string.Empty,
                    CreatedAt = note.CreatedAt,
                },
            })
            .ToList();

        var movements = (
            await movementRepository.GetMovementsByAnimalAsync(request.AnimalId)
        ).ToList();

        if (movements.Count > 0)
        {
            var userIds = movements.Select(m => m.UserId).Distinct().ToList();
            var lotIds = movements
                .SelectMany(m => new[] { m.FromLotId, m.ToLotId })
                .Where(id => id.HasValue)
                .Select(id => id!.Value)
                .Distinct()
                .ToList();

            var users = (await userRepository.FindAsync(u => userIds.Contains(u.Id))).ToDictionary(
                u => u.Id,
                u => u.Name
            );

            var lots = new Dictionary<int, string>();
            if (lotIds.Count > 0)
            {
                var lotsResult = await lotRepository.FindAsync(l => lotIds.Contains(l.Id));
                lots = lotsResult.ToDictionary(l => l.Id, l => l.Name);
            }

            timelineItems.AddRange(
                movements.Select(movement => new AnimalTimelineItemDto
                {
                    Type = "movement",
                    OccurredAt = movement.At,
                    Movement = new MovementDto
                    {
                        Id = movement.Id,
                        AnimalId = movement.AnimalId,
                        AnimalName = animal.Name,
                        FromLotId = movement.FromLotId,
                        FromLotName = movement.FromLotId.HasValue
                            ? lots.GetValueOrDefault(movement.FromLotId.Value)
                            : null,
                        ToLotId = movement.ToLotId,
                        ToLotName = movement.ToLotId.HasValue
                            ? lots.GetValueOrDefault(movement.ToLotId.Value)
                            : null,
                        At = movement.At,
                        Reason = movement.Reason,
                        UserId = movement.UserId,
                        UserName = users.GetValueOrDefault(movement.UserId, string.Empty),
                        CreatedAt = movement.CreatedAt,
                    },
                })
            );
        }

        var checklistItems = (
            await checklistRepository.GetItemsByAnimalIdAsync(request.AnimalId)
        ).ToList();

        timelineItems.AddRange(
            checklistItems.Select(item => new AnimalTimelineItemDto
            {
                Type = "checklist",
                OccurredAt = item.Checklist.Date,
                ChecklistItem = new AnimalChecklistTimelineDto
                {
                    ChecklistId = item.ChecklistId,
                    ChecklistItemId = item.Id,
                    Date = item.Checklist.Date,
                    LotId = item.Checklist.LotId,
                    LotName = item.Checklist.Lot?.Name,
                    Present = item.Present,
                    Condition = item.Condition,
                    Notes = item.Notes,
                },
            })
        );

        var retirement = await animalRetirementRepository.GetByAnimalIdAsync(request.AnimalId);
        if (retirement != null)
        {
            timelineItems.Add(
                new AnimalTimelineItemDto
                {
                    Type = "retirement",
                    OccurredAt = retirement.At,
                    Retirement = AnimalRetirementDto.From(retirement),
                }
            );
        }

        return timelineItems.OrderByDescending(i => i.OccurredAt);
    }
}
