using AgroLink.Application.Features.Checklists.DTOs;
using AgroLink.Application.Interfaces;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using MediatR;

namespace AgroLink.Application.Features.Checklists.Queries.GetByLot;

public class GetChecklistsByLotQueryHandler(
    IChecklistRepository checklistRepository,
    IRepository<ChecklistItem> checklistItemRepository,
    IUserRepository userRepository,
    IAnimalRepository animalRepository,
    ILotRepository lotRepository,
    ICurrentUserService currentUserService
) : IRequestHandler<GetChecklistsByLotQuery, IEnumerable<ChecklistDto>>
{
    public async Task<IEnumerable<ChecklistDto>> Handle(
        GetChecklistsByLotQuery request,
        CancellationToken cancellationToken
    )
    {
        // Security check: ensure lot belongs to the current farm context
        if (currentUserService.CurrentFarmId.HasValue)
        {
            var lot = await lotRepository.GetLotWithPaddockAsync(request.LotId);
            if (lot?.Paddock?.FarmId != currentUserService.CurrentFarmId.Value)
            {
                return [];
            }
        }

        var checklists = (await checklistRepository.GetByLotIdAsync(request.LotId)).ToList();
        if (checklists.Count == 0)
        {
            return [];
        }

        // Batch-fetch all related data upfront
        var checklistIds = checklists.Select(c => c.Id).ToList();
        var userIds = checklists.Select(c => c.UserId).Distinct().ToList();
        var lotIds = checklists.Select(c => c.LotId).Distinct().ToList();

        var users = (await userRepository.FindAsync(u => userIds.Contains(u.Id)))
            .ToDictionary(u => u.Id);
        var lots = (await lotRepository.FindAsync(l => lotIds.Contains(l.Id)))
            .ToDictionary(l => l.Id);
        var allItems = (
            await checklistItemRepository.FindAsync(ci => checklistIds.Contains(ci.ChecklistId))
        ).ToList();

        // Batch-fetch animals from all items
        var animalIds = allItems.Select(i => i.AnimalId).Distinct().ToList();
        var animals = animalIds.Count > 0
            ? (await animalRepository.FindAsync(a => animalIds.Contains(a.Id)))
                .ToDictionary(a => a.Id)
            : new Dictionary<int, Animal>();

        // Batch-fetch animal lots
        var animalLotIds = animals.Values.Select(a => a.LotId).Distinct().ToList();
        var animalLots = animalLotIds.Count > 0
            ? (await lotRepository.FindAsync(l => animalLotIds.Contains(l.Id)))
                .ToDictionary(l => l.Id)
            : new Dictionary<int, Lot>();

        // Group items by checklist
        var itemsByChecklist = allItems
            .GroupBy(i => i.ChecklistId)
            .ToDictionary(g => g.Key, g => g.ToList());

        // Map to DTOs using pre-fetched data
        return checklists
            .Select(checklist =>
            {
                users.TryGetValue(checklist.UserId, out var user);
                lots.TryGetValue(checklist.LotId, out var lot);
                var items = itemsByChecklist.GetValueOrDefault(checklist.Id, []);

                var itemDtos = items
                    .Select(item =>
                    {
                        animals.TryGetValue(item.AnimalId, out var animal);
                        animalLots.TryGetValue(animal?.LotId ?? 0, out var animalLot);
                        return new ChecklistItemDto
                        {
                            Id = item.Id,
                            AnimalId = item.AnimalId,
                            AnimalCuia = animal?.Cuia,
                            AnimalName = animal?.Name,
                            AnimalLotId = animal?.LotId,
                            AnimalLotName = animalLot?.Name,
                            Present = item.Present,
                            Condition = item.Condition,
                            Notes = item.Notes,
                        };
                    })
                    .ToList();

                return new ChecklistDto
                {
                    Id = checklist.Id,
                    LotId = checklist.LotId,
                    LotName = lot?.Name,
                    Date = checklist.Date,
                    UserId = checklist.UserId,
                    UserName = user?.Name ?? "",
                    Notes = checklist.Notes,
                    Items = itemDtos,
                    CreatedAt = checklist.CreatedAt,
                };
            })
            .ToList();
    }
}
