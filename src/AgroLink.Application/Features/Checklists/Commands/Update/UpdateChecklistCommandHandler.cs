using AgroLink.Application.Features.Checklists.DTOs;
using AgroLink.Application.Features.Photos.DTOs;
using AgroLink.Application.Interfaces;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using MediatR;

namespace AgroLink.Application.Features.Checklists.Commands.Update;

public class UpdateChecklistCommandHandler(
    IChecklistRepository checklistRepository,
    IRepository<ChecklistItem> checklistItemRepository, // Using generic repository for ChecklistItem
    IUserRepository userRepository,
    IAnimalRepository animalRepository,
    IPhotoRepository photoRepository,
    ILotRepository lotRepository,
    IPaddockRepository paddockRepository,
    IUnitOfWork unitOfWork
) : IRequestHandler<UpdateChecklistCommand, ChecklistDto>
{
    public async Task<ChecklistDto> Handle(
        UpdateChecklistCommand request,
        CancellationToken cancellationToken
    )
    {
        var checklist = await checklistRepository.GetByIdAsync(request.Id);
        if (checklist == null)
        {
            throw new ArgumentException("Checklist not found");
        }

        var dto = request.Dto;
        checklist.ScopeType = dto.ScopeType;
        checklist.ScopeId = dto.ScopeId;
        checklist.Date = dto.Date;
        checklist.Notes = dto.Notes;
        checklist.UpdatedAt = DateTime.UtcNow;

        checklistRepository.Update(checklist);

        // Update checklist items: remove existing, add new
        var existingItems = await checklistItemRepository.FindAsync(ci =>
            ci.ChecklistId == request.Id
        );
        checklistItemRepository.RemoveRange(existingItems);

        foreach (var itemDto in dto.Items)
        {
            var item = new ChecklistItem
            {
                ChecklistId = request.Id,
                AnimalId = itemDto.AnimalId,
                Present = itemDto.Present,
                Condition = itemDto.Condition,
                Notes = itemDto.Notes,
            };
            await checklistItemRepository.AddAsync(item);
        }

        await unitOfWork.SaveChangesAsync(); // Save all changes
        return await MapToDtoAsync(checklist);
    }

    private async Task<ChecklistDto> MapToDtoAsync(Checklist checklist)
    {
        var user = await userRepository.GetByIdAsync(checklist.UserId);
        var items = await checklistItemRepository.FindAsync(ci => ci.ChecklistId == checklist.Id);
        var photos = await photoRepository.GetPhotosByEntityAsync("CHECKLIST", checklist.Id);

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
