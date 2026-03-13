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
    ICurrentUserService currentUserService,
    IUnitOfWork unitOfWork
) : IRequestHandler<UpdateChecklistCommand, ChecklistDto>
{
    public async Task<ChecklistDto> Handle(
        UpdateChecklistCommand request,
        CancellationToken cancellationToken
    )
    {
        var checklist =
            await checklistRepository.GetByIdAsync(request.Id)
            ?? throw new NotFoundException($"Checklist with ID {request.Id} was not found.");

        var farmId =
            currentUserService.CurrentFarmId
            ?? throw new UnauthorizedAccessException(
                "Farm context is required to update a checklist."
            );

        // Validate existing checklist belongs to farm
        var existingLot =
            await lotRepository.GetLotWithPaddockAsync(checklist.LotId)
            ?? throw new NotFoundException($"Lot with ID {checklist.LotId} was not found.");
        if (existingLot.Paddock?.FarmId != farmId)
        {
            throw new ForbiddenAccessException("You do not have access to this checklist.");
        }

        var dto = request.Dto;

        // Validate new lot belongs to farm
        var newLot =
            await lotRepository.GetLotWithPaddockAsync(dto.LotId)
            ?? throw new NotFoundException($"Lot with ID {dto.LotId} was not found.");
        if (newLot.Paddock?.FarmId != farmId)
        {
            throw new ForbiddenAccessException(
                "The target lot belongs to a different farm context."
            );
        }

        // Validate items not empty
        if (dto.Items == null || dto.Items.Count == 0)
        {
            throw new ArgumentException("At least one checklist item is required.");
        }

        // Validate all animal IDs exist
        var animalIds = dto.Items.Select(i => i.AnimalId).Distinct().ToList();
        var animals = (await animalRepository.FindAsync(a => animalIds.Contains(a.Id))).ToList();
        if (animals.Count != animalIds.Count)
        {
            var foundIds = animals.Select(a => a.Id).ToHashSet();
            var missingIds = animalIds.Where(id => !foundIds.Contains(id)).ToList();
            throw new NotFoundException(
                $"Animals with IDs [{string.Join(", ", missingIds)}] were not found."
            );
        }

        var animalsDict = animals.ToDictionary(a => a.Id);

        // Batch-fetch animal lots for DTO mapping
        var animalLotIds = animals.Select(a => a.LotId).Distinct().ToList();
        var animalLots = (
            await lotRepository.FindAsync(l => animalLotIds.Contains(l.Id))
        ).ToDictionary(l => l.Id);

        // Update checklist
        checklist.LotId = dto.LotId;
        checklist.Date = dto.Date;
        checklist.Notes = dto.Notes;
        checklist.UpdatedAt = DateTime.UtcNow;

        checklistRepository.Update(checklist);

        // Replace items
        var existingItems = await checklistItemRepository.FindAsync(ci =>
            ci.ChecklistId == request.Id
        );
        checklistItemRepository.RemoveRange(existingItems);

        var newItems = dto
            .Items.Select(itemDto => new ChecklistItem
            {
                ChecklistId = request.Id,
                AnimalId = itemDto.AnimalId,
                Present = itemDto.Present,
                Condition = itemDto.Condition,
                Notes = itemDto.Notes,
            })
            .ToList();

        await checklistItemRepository.AddRangeAsync(newItems);

        await unitOfWork.SaveChangesAsync();

        // Map to DTO
        var user = await userRepository.GetByIdAsync(checklist.UserId);

        var itemDtos = newItems
            .Select(item =>
            {
                var animal = animalsDict[item.AnimalId];
                animalLots.TryGetValue(animal.LotId, out var animalLot);
                return new ChecklistItemDto
                {
                    Id = item.Id,
                    AnimalId = item.AnimalId,
                    AnimalCuia = animal.Cuia,
                    AnimalName = animal.Name,
                    AnimalLotId = animal.LotId,
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
            LotName = newLot.Name,
            Date = checklist.Date,
            UserId = checklist.UserId,
            UserName = user?.Name ?? "",
            Notes = checklist.Notes,
            Items = itemDtos,
            CreatedAt = checklist.CreatedAt,
        };
    }
}
