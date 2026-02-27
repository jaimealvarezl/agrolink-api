using AgroLink.Application.Common.Exceptions;
using AgroLink.Application.Features.Checklists.DTOs;
using AgroLink.Application.Interfaces;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using MediatR;

namespace AgroLink.Application.Features.Checklists.Commands.Update;

public class UpdateChecklistCommandHandler(
    IChecklistRepository checklistRepository,
    IRepository<ChecklistItem> checklistItemRepository,
    IUserRepository userRepository,
    IAnimalRepository animalRepository,
    ILotRepository lotRepository,
    IPaddockRepository paddockRepository,
    ICurrentUserService currentUserService,
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

        // Security check: ensure checklist and new target scope belong to the current farm context
        if (currentUserService.CurrentFarmId.HasValue)
        {
            // Check existing checklist farm
            int? existingFarmId = null;
            if (checklist.ScopeType == "LOT")
            {
                var lot = await lotRepository.GetLotWithPaddockAsync(checklist.ScopeId);
                existingFarmId = lot?.Paddock?.FarmId;
            }
            else if (checklist.ScopeType == "PADDOCK")
            {
                var paddock = await paddockRepository.GetByIdAsync(checklist.ScopeId);
                existingFarmId = paddock?.FarmId;
            }

            if (existingFarmId != null && existingFarmId != currentUserService.CurrentFarmId.Value)
            {
                throw new ForbiddenAccessException("You do not have access to this checklist");
            }

            // Check new target scope farm
            var dto = request.Dto;
            int? newFarmId = null;
            if (dto.ScopeType == "LOT")
            {
                var lot = await lotRepository.GetLotWithPaddockAsync(dto.ScopeId);
                newFarmId = lot?.Paddock?.FarmId;
            }
            else if (dto.ScopeType == "PADDOCK")
            {
                var paddock = await paddockRepository.GetByIdAsync(dto.ScopeId);
                newFarmId = paddock?.FarmId;
            }

            if (newFarmId != null && newFarmId != currentUserService.CurrentFarmId.Value)
            {
                throw new ForbiddenAccessException(
                    "The target scope belongs to a different farm context."
                );
            }
        }

        var dtoUpdate = request.Dto;
        checklist.ScopeType = dtoUpdate.ScopeType;
        checklist.ScopeId = dtoUpdate.ScopeId;
        checklist.Date = dtoUpdate.Date;
        checklist.Notes = dtoUpdate.Notes;
        checklist.UpdatedAt = DateTime.UtcNow;

        checklistRepository.Update(checklist);

        var existingItems = await checklistItemRepository.FindAsync(ci =>
            ci.ChecklistId == request.Id
        );
        checklistItemRepository.RemoveRange(existingItems);

        foreach (var itemDto in dtoUpdate.Items)
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

        await unitOfWork.SaveChangesAsync();
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
