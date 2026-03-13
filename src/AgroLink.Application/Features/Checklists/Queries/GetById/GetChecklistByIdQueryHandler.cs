using AgroLink.Application.Features.Checklists.DTOs;
using AgroLink.Application.Interfaces;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using MediatR;

namespace AgroLink.Application.Features.Checklists.Queries.GetById;

public class GetChecklistByIdQueryHandler(
    IChecklistRepository checklistRepository,
    IRepository<ChecklistItem> checklistItemRepository,
    IUserRepository userRepository,
    IAnimalRepository animalRepository,
    ILotRepository lotRepository,
    ICurrentUserService currentUserService
) : IRequestHandler<GetChecklistByIdQuery, ChecklistDto?>
{
    public async Task<ChecklistDto?> Handle(
        GetChecklistByIdQuery request,
        CancellationToken cancellationToken
    )
    {
        var checklist = await checklistRepository.GetByIdAsync(request.Id);
        if (checklist == null)
        {
            return null;
        }

        // Security check: ensure checklist belongs to the current farm context
        if (currentUserService.CurrentFarmId.HasValue)
        {
            var lot = await lotRepository.GetLotWithPaddockAsync(checklist.LotId);
            if (lot?.Paddock?.FarmId != currentUserService.CurrentFarmId.Value)
            {
                return null;
            }
        }

        return await MapToDtoAsync(checklist);
    }

    private async Task<ChecklistDto> MapToDtoAsync(Checklist checklist)
    {
        var user = await userRepository.GetByIdAsync(checklist.UserId);
        var lot = await lotRepository.GetByIdAsync(checklist.LotId);
        var items = (
            await checklistItemRepository.FindAsync(ci => ci.ChecklistId == checklist.Id)
        ).ToList();

        // Batch-fetch animals
        var animalIds = items.Select(i => i.AnimalId).Distinct().ToList();
        var animals = (
            await animalRepository.FindAsync(a => animalIds.Contains(a.Id))
        ).ToDictionary(a => a.Id);

        // Batch-fetch animal lots
        var animalLotIds = animals.Values.Select(a => a.LotId).Distinct().ToList();
        var animalLots = (
            await lotRepository.FindAsync(l => animalLotIds.Contains(l.Id))
        ).ToDictionary(l => l.Id);

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
    }
}
