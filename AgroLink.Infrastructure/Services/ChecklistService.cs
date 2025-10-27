using AgroLink.Core.DTOs;
using AgroLink.Core.Entities;
using AgroLink.Core.Interfaces;
using AgroLink.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AgroLink.Infrastructure.Services;

public class ChecklistService(AgroLinkDbContext context) : IChecklistService
{
    public async Task<ChecklistDto?> GetByIdAsync(int id)
    {
        var checklist = await context.Checklists.FindAsync(id);
        if (checklist == null)
        {
            return null;
        }

        return await MapToDtoAsync(checklist);
    }

    public async Task<IEnumerable<ChecklistDto>> GetAllAsync()
    {
        var checklists = await context.Checklists.ToListAsync();
        var result = new List<ChecklistDto>();

        foreach (var checklist in checklists)
        {
            result.Add(await MapToDtoAsync(checklist));
        }

        return result;
    }

    public async Task<IEnumerable<ChecklistDto>> GetByScopeAsync(string scopeType, int scopeId)
    {
        var checklists = await context
            .Checklists.Where(c => c.ScopeType == scopeType && c.ScopeId == scopeId)
            .ToListAsync();
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

        context.Checklists.Add(checklist);
        await context.SaveChangesAsync();

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
            context.ChecklistItems.Add(item);
        }

        await context.SaveChangesAsync();
        return await MapToDtoAsync(checklist);
    }

    public async Task<ChecklistDto> UpdateAsync(int id, CreateChecklistDto dto)
    {
        var checklist = await context.Checklists.FindAsync(id);
        if (checklist == null)
        {
            throw new ArgumentException("Checklist not found");
        }

        checklist.ScopeType = dto.ScopeType;
        checklist.ScopeId = dto.ScopeId;
        checklist.Date = dto.Date;
        checklist.Notes = dto.Notes;
        checklist.UpdatedAt = DateTime.UtcNow;

        context.Checklists.Update(checklist);

        // Update checklist items
        var existingItems = await context
            .ChecklistItems.Where(ci => ci.ChecklistId == id)
            .ToListAsync();
        context.ChecklistItems.RemoveRange(existingItems);

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
            context.ChecklistItems.Add(item);
        }

        await context.SaveChangesAsync();
        return await MapToDtoAsync(checklist);
    }

    public async Task DeleteAsync(int id)
    {
        var checklist = await context.Checklists.FindAsync(id);
        if (checklist == null)
        {
            throw new ArgumentException("Checklist not found");
        }

        context.Checklists.Remove(checklist);
        await context.SaveChangesAsync();
    }

    private async Task<ChecklistDto> MapToDtoAsync(Checklist checklist)
    {
        var user = await context.Users.FindAsync(checklist.UserId);
        var items = await context
            .ChecklistItems.Where(ci => ci.ChecklistId == checklist.Id)
            .ToListAsync();
        var photos = await context
            .Photos.Where(p => p.EntityType == "CHECKLIST" && p.EntityId == checklist.Id)
            .ToListAsync();

        var itemDtos = new List<ChecklistItemDto>();
        foreach (var item in items)
        {
            var animal = await context.Animals.FindAsync(item.AnimalId);
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
            var lot = await context.Lots.FindAsync(checklist.ScopeId);
            scopeName = lot?.Name;
        }
        else if (checklist.ScopeType == "PADDOCK")
        {
            var paddock = await context.Paddocks.FindAsync(checklist.ScopeId);
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
