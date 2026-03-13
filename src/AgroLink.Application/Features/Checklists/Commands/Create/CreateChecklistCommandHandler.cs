using AgroLink.Application.Common.Exceptions;
using AgroLink.Application.Features.Checklists.DTOs;
using AgroLink.Application.Interfaces;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using MediatR;

namespace AgroLink.Application.Features.Checklists.Commands.Create;

public class CreateChecklistCommandHandler(
    IChecklistRepository checklistRepository,
    IUserRepository userRepository,
    IAnimalRepository animalRepository,
    ILotRepository lotRepository,
    ICurrentUserService currentUserService,
    IUnitOfWork unitOfWork
) : IRequestHandler<CreateChecklistCommand, ChecklistDto>
{
    public async Task<ChecklistDto> Handle(
        CreateChecklistCommand request,
        CancellationToken cancellationToken
    )
    {
        var dto = request.Dto;

        // Require farm context
        var farmId =
            currentUserService.CurrentFarmId
            ?? throw new UnauthorizedAccessException(
                "Farm context is required to create a checklist."
            );

        // Validate lot belongs to farm
        var lot =
            await lotRepository.GetLotWithPaddockAsync(dto.LotId)
            ?? throw new NotFoundException($"Lot with ID {dto.LotId} was not found.");
        if (lot.Paddock?.FarmId != farmId)
        {
            throw new ForbiddenAccessException(
                "You do not have access to create checklists in this farm context."
            );
        }

        // Filter null items and validate not empty
        var validItems = dto.Items.ToList();
        if (validItems.Count == 0)
        {
            throw new ArgumentException("At least one checklist item is required.");
        }

        var animalIds = validItems.Select(i => i.AnimalId).Distinct().ToList();
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

        // Create checklist with items using EF Core relationship fix-up
        var checklist = new Checklist
        {
            LotId = dto.LotId,
            Date = dto.Date,
            UserId = request.UserId,
            Notes = dto.Notes,
        };

        foreach (var itemDto in validItems)
        {
            checklist.ChecklistItems.Add(
                new ChecklistItem
                {
                    AnimalId = itemDto.AnimalId,
                    Present = itemDto.Present,
                    Condition = itemDto.Condition,
                    Notes = itemDto.Notes,
                }
            );
        }

        await checklistRepository.AddAsync(checklist);
        await unitOfWork.SaveChangesAsync();

        // Map to DTO using pre-fetched data
        var user = await userRepository.GetByIdAsync(checklist.UserId);

        var itemDtos = checklist
            .ChecklistItems.Select(item =>
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
            LotName = lot.Name,
            Date = checklist.Date,
            UserId = checklist.UserId,
            UserName = user?.Name ?? "",
            Notes = checklist.Notes,
            Items = itemDtos,
            CreatedAt = checklist.CreatedAt,
        };
    }
}
