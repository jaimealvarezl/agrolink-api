using AgroLink.Application.Common.Exceptions;
using AgroLink.Application.Features.Checklists.DTOs;
using AgroLink.Application.Interfaces;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using MediatR;

namespace AgroLink.Application.Features.Checklists.Commands.Create;

public class CreateChecklistCommandHandler(
    IChecklistRepository checklistRepository,
    IRepository<ChecklistItem> checklistItemRepository,
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

        // Validate items not empty
        if (dto.Items == null || dto.Items.Count == 0)
        {
            throw new ArgumentException("At least one checklist item is required.");
        }

        // Validate all animal IDs exist and batch-fetch for DTO mapping
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

        // Wrap in transaction
        await unitOfWork.BeginTransactionAsync();
        try
        {
            var checklist = new Checklist
            {
                LotId = dto.LotId,
                Date = dto.Date,
                UserId = request.UserId,
                Notes = dto.Notes,
            };

            await checklistRepository.AddAsync(checklist);
            await unitOfWork.SaveChangesAsync();

            var checklistItems = new List<ChecklistItem>();
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
                checklistItems.Add(item);
            }

            await checklistItemRepository.AddRangeAsync(checklistItems);
            await unitOfWork.SaveChangesAsync();

            await unitOfWork.CommitTransactionAsync();

            // Map to DTO using pre-fetched data
            var user = await userRepository.GetByIdAsync(checklist.UserId);

            var itemDtos = checklistItems
                .Select(item =>
                {
                    animalsDict.TryGetValue(item.AnimalId, out var animal);
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
                LotName = lot.Name,
                Date = checklist.Date,
                UserId = checklist.UserId,
                UserName = user?.Name ?? "",
                Notes = checklist.Notes,
                Items = itemDtos,
                CreatedAt = checklist.CreatedAt,
            };
        }
        catch
        {
            await unitOfWork.RollbackTransactionAsync();
            throw;
        }
    }
}
