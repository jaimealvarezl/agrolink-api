using AgroLink.Core.DTOs;
using AgroLink.Core.Entities;
using AgroLink.Core.Interfaces;

namespace AgroLink.Infrastructure.Services;

public class ChecklistService : IChecklistService
{
    private readonly IUnitOfWork _unitOfWork;

    public ChecklistService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ChecklistDto?> GetByIdAsync(int id)
    {
        var checklist = await _unitOfWork.Checklists.GetByIdAsync(id);
        if (checklist == null) return null;

        return await MapToDtoAsync(checklist);
    }

    public async Task<IEnumerable<ChecklistDto>> GetAllAsync()
    {
        var checklists = await _unitOfWork.Checklists.GetAllAsync();
        var result = new List<ChecklistDto>();

        foreach (var checklist in checklists)
        {
            result.Add(await MapToDtoAsync(checklist));
        }

        return result;
    }

    public async Task<IEnumerable<ChecklistDto>> GetByScopeAsync(string scopeType, int scopeId)
    {
        var checklists = await _unitOfWork.Checklists.FindAsync(c => c.ScopeType == scopeType && c.ScopeId == scopeId);
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
            Notes = dto.Notes
        };

        await _unitOfWork.Checklists.AddAsync(checklist);
        await _unitOfWork.SaveChangesAsync();

        // Add checklist items
        foreach (var itemDto in dto.Items)
        {
            var item = new ChecklistItem
            {
                ChecklistId = checklist.Id,
                AnimalId = itemDto.AnimalId,
                Present = itemDto.Present,
                Condition = itemDto.Condition,
                Notes = itemDto.Notes
            };
            await _unitOfWork.ChecklistItems.AddAsync(item);
        }

        await _unitOfWork.SaveChangesAsync();
        return await MapToDtoAsync(checklist);
    }

    public async Task<ChecklistDto> UpdateAsync(int id, CreateChecklistDto dto)
    {
        var checklist = await _unitOfWork.Checklists.GetByIdAsync(id);
        if (checklist == null) throw new ArgumentException("Checklist not found");

        checklist.ScopeType = dto.ScopeType;
        checklist.ScopeId = dto.ScopeId;
        checklist.Date = dto.Date;
        checklist.Notes = dto.Notes;
        checklist.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Checklists.UpdateAsync(checklist);

        // Update checklist items
        var existingItems = await _unitOfWork.ChecklistItems.FindAsync(ci => ci.ChecklistId == id);
        foreach (var existingItem in existingItems)
        {
            await _unitOfWork.ChecklistItems.DeleteAsync(existingItem);
        }

        foreach (var itemDto in dto.Items)
        {
            var item = new ChecklistItem
            {
                ChecklistId = id,
                AnimalId = itemDto.AnimalId,
                Present = itemDto.Present,
                Condition = itemDto.Condition,
                Notes = itemDto.Notes
            };
            await _unitOfWork.ChecklistItems.AddAsync(item);
        }

        await _unitOfWork.SaveChangesAsync();
        return await MapToDtoAsync(checklist);
    }

    public async Task DeleteAsync(int id)
    {
        var checklist = await _unitOfWork.Checklists.GetByIdAsync(id);
        if (checklist == null) throw new ArgumentException("Checklist not found");

        await _unitOfWork.Checklists.DeleteAsync(checklist);
        await _unitOfWork.SaveChangesAsync();
    }

    private async Task<ChecklistDto> MapToDtoAsync(Checklist checklist)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(checklist.UserId);
        var items = await _unitOfWork.ChecklistItems.FindAsync(ci => ci.ChecklistId == checklist.Id);
        var photos = await _unitOfWork.Photos.FindAsync(p => p.EntityType == "CHECKLIST" && p.EntityId == checklist.Id);

        var itemDtos = new List<ChecklistItemDto>();
        foreach (var item in items)
        {
            var animal = await _unitOfWork.Animals.GetByIdAsync(item.AnimalId);
            itemDtos.Add(new ChecklistItemDto
            {
                Id = item.Id,
                AnimalId = item.AnimalId,
                AnimalTag = animal?.Tag ?? "",
                AnimalName = animal?.Name,
                Present = item.Present,
                Condition = item.Condition,
                Notes = item.Notes
            });
        }

        var photoDtos = photos.Select(p => new PhotoDto
        {
            Id = p.Id,
            EntityType = p.EntityType,
            EntityId = p.EntityId,
            UriLocal = p.UriLocal,
            UriRemote = p.UriRemote,
            Uploaded = p.Uploaded,
            Description = p.Description,
            CreatedAt = p.CreatedAt
        }).ToList();

        string? scopeName = null;
        if (checklist.ScopeType == "LOT")
        {
            var lot = await _unitOfWork.Lots.GetByIdAsync(checklist.ScopeId);
            scopeName = lot?.Name;
        }
        else if (checklist.ScopeType == "PADDOCK")
        {
            var paddock = await _unitOfWork.Paddocks.GetByIdAsync(checklist.ScopeId);
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
            CreatedAt = checklist.CreatedAt
        };
    }
}