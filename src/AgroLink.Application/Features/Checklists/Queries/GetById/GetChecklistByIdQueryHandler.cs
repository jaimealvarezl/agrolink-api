using AgroLink.Application.Features.Checklists.DTOs;
using AgroLink.Application.Interfaces;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using MediatR;

namespace AgroLink.Application.Features.Checklists.Queries.GetById;

public class GetChecklistByIdQueryHandler(
    IChecklistRepository checklistRepository,
    IRepository<ChecklistItem> checklistItemRepository, // Using generic repository for ChecklistItem
    IUserRepository userRepository,
    IAnimalRepository animalRepository,
    ILotRepository lotRepository,
    IPaddockRepository paddockRepository
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

        return await MapToDtoAsync(checklist);
    }

    private async Task<ChecklistDto> MapToDtoAsync(Checklist checklist)
    {
        var user = await userRepository.GetByIdAsync(checklist.UserId);
        var items = await checklistItemRepository.FindAsync(ci => ci.ChecklistId == checklist.Id);

        var itemDtos = new List<ChecklistItemDto>();
        foreach (var item in items)
        {
            var animal = await animalRepository.GetByIdAsync(item.AnimalId);
            itemDtos.Add(
                new ChecklistItemDto
                {
                    Id = item.Id,
                    AnimalId = item.AnimalId,
                    AnimalCuia = animal?.Cuia,
                    AnimalName = animal?.Name,
                    Present = item.Present,
                    Condition = item.Condition,
                    Notes = item.Notes,
                }
            );
        }

        string? scopeName = null;
        if (checklist.ScopeType == "LOT")
        {
            var lot = await lotRepository.GetByIdAsync(checklist.ScopeId);
            scopeName = lot?.Name;
        }
        else if (checklist.ScopeType == "PADDOCK")
        {
            var paddock = await paddockRepository.GetByIdAsync(checklist.ScopeId);
            scopeName = paddock?.Name;
        }

        return new ChecklistDto
        {
            Id = checklist.Id,
            ScopeType = checklist.ScopeType,
            ScopeId = checklist.ScopeId,
            ScopeName = scopeName,
            Date = checklist.Date,
            UserId = checklist.UserId,
            UserName = user?.Name ?? "",
            Notes = checklist.Notes,
            Items = itemDtos,
            CreatedAt = checklist.CreatedAt,
        };
    }
}
