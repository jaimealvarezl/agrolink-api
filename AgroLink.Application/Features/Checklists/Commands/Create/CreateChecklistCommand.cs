using AgroLink.Application.DTOs;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using MediatR;

namespace AgroLink.Application.Features.Checklists.Commands.Create;

public record CreateChecklistCommand(CreateChecklistDto Dto, int UserId) : IRequest<ChecklistDto>;

public class CreateChecklistCommandHandler(
    IChecklistRepository checklistRepository,
    IRepository<ChecklistItem> checklistItemRepository,
    IUserRepository userRepository,
    IAnimalRepository animalRepository,
    IPhotoRepository photoRepository,
    ILotRepository lotRepository,
    IPaddockRepository paddockRepository
) : IRequestHandler<CreateChecklistCommand, ChecklistDto>
{
    public async Task<ChecklistDto> Handle(
        CreateChecklistCommand request,
        CancellationToken cancellationToken
    )
    {
        var dto = request.Dto;
        var checklist = new Checklist
        {
            ScopeType = dto.ScopeType,
            ScopeId = dto.ScopeId,
            Date = dto.Date,
            UserId = request.UserId,
            Notes = dto.Notes,
        };

        await checklistRepository.AddAsync(checklist);
        await checklistRepository.SaveChangesAsync();

        foreach (var itemDto in dto.Items)
        {
            var item = new ChecklistItem
            {
                ChecklistId = checklist.Id,
                AnimalId = itemDto.AnimalId,
                Present = itemDto.Present,
                Condition = itemDto.Condition,
                Notes = itemDto.Notes,
            };
            await checklistItemRepository.AddAsync(item);
        }

        await checklistItemRepository.SaveChangesAsync();

        var user = await userRepository.GetByIdAsync(checklist.UserId);
        var items = await checklistItemRepository.FindAsync(ci => ci.ChecklistId == checklist.Id);
        var photos = await photoRepository.GetByEntityAsync("CHECKLIST", checklist.Id);

        var itemDtos = new List<ChecklistItemDto>();
        foreach (var item in items)
        {
            var animal = await animalRepository.GetByIdAsync(item.AnimalId);
            itemDtos.Add(
                new ChecklistItemDto
                {
                    Id = item.Id,
                    AnimalId = item.AnimalId,
                    AnimalTag = animal?.Tag ?? "",
                    AnimalName = animal?.Name,
                    Present = item.Present,
                    Condition = item.Condition,
                    Notes = item.Notes,
                }
            );
        }

        var photoDtos = photos
            .Select(p => new PhotoDto
            {
                Id = p.Id,
                EntityType = p.EntityType,
                EntityId = p.EntityId,
                UriLocal = p.UriLocal,
                UriRemote = p.UriRemote,
                Uploaded = p.Uploaded,
                Description = p.Description,
                CreatedAt = p.CreatedAt,
            })
            .ToList();

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
            Photos = photoDtos,
            CreatedAt = checklist.CreatedAt,
        };
    }
}
