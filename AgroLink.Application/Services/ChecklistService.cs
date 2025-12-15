using AgroLink.Application.DTOs;
using AgroLink.Application.Interfaces;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;

// Removed using AgroLink.Infrastructure.Data;
// Removed using Microsoft.EntityFrameworkCore;

// Keep old namespace for now, will move later
namespace AgroLink.Application.Services;

public class ChecklistService(
    IChecklistRepository checklistRepository,
    IRepository<ChecklistItem> checklistItemRepository, // Using generic repository for ChecklistItem
    IUserRepository userRepository,
    IAnimalRepository animalRepository,
    IPhotoRepository photoRepository,
    ILotRepository lotRepository,
    IPaddockRepository paddockRepository
) : IChecklistService
{
    public async Task<ChecklistDto?> GetByIdAsync(int id)
    {
        var checklist = await checklistRepository.GetByIdAsync(id);
        if (checklist == null)
        {
            return null;
        }

        return await MapToDtoAsync(checklist);
    }

    public async Task<IEnumerable<ChecklistDto>> GetAllAsync()
    {
        var checklists = await checklistRepository.GetAllAsync();
        var result = new List<ChecklistDto>();

        foreach (var checklist in checklists)
        {
            result.Add(await MapToDtoAsync(checklist));
        }

        return result;
    }

    public async Task<IEnumerable<ChecklistDto>> GetByScopeAsync(string scopeType, int scopeId)
    {
        var checklists = await checklistRepository.GetByScopeAsync(scopeType, scopeId);
        var result = new List<ChecklistDto>();

        foreach (var checklist in checklists)
        {
            result.Add(await MapToDtoAsync(checklist));
        }

        return result;
    }

    public async Task<ChecklistDto> CreateAsync(CreateChecklistDto dto, int userId)
    {
        var checklist = new Checklist
        {
            ScopeType = dto.ScopeType,
            ScopeId = dto.ScopeId,
            Date = dto.Date,
            UserId = userId,
            Notes = dto.Notes,
        };

        await checklistRepository.AddAsync(checklist);
        await checklistRepository.SaveChangesAsync(); // Save checklist to get its Id

        // Add checklist items
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

        await checklistItemRepository.SaveChangesAsync(); // Save items
        return await MapToDtoAsync(checklist);
    }

    public async Task<ChecklistDto> UpdateAsync(int id, CreateChecklistDto dto)
    {
        var checklist = await checklistRepository.GetByIdAsync(id);
        if (checklist == null)
        {
            throw new ArgumentException("Checklist not found");
        }

        checklist.ScopeType = dto.ScopeType;
        checklist.ScopeId = dto.ScopeId;
        checklist.Date = dto.Date;
        checklist.Notes = dto.Notes;
        checklist.UpdatedAt = DateTime.UtcNow;

        checklistRepository.Update(checklist);

        // Update checklist items: remove existing, add new
        var existingItems = await checklistItemRepository.FindAsync(ci => ci.ChecklistId == id);
        checklistItemRepository.RemoveRange(existingItems);

        foreach (var itemDto in dto.Items)
        {
            var item = new ChecklistItem
            {
                ChecklistId = id,
                AnimalId = itemDto.AnimalId,
                Present = itemDto.Present,
                Condition = itemDto.Condition,
                Notes = itemDto.Notes,
            };
            await checklistItemRepository.AddAsync(item);
        }

        await checklistRepository.SaveChangesAsync(); // Save all changes
        return await MapToDtoAsync(checklist);
    }

    public async Task DeleteAsync(int id)
    {
        var checklist = await checklistRepository.GetByIdAsync(id);
        if (checklist == null)
        {
            throw new ArgumentException("Checklist not found");
        }

        checklistRepository.Remove(checklist);
        await checklistRepository.SaveChangesAsync();
    }

    private async Task<ChecklistDto> MapToDtoAsync(Checklist checklist)
    {
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
