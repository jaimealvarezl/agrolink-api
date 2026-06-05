using AgroLink.Application.Common.Exceptions;
using AgroLink.Application.Features.ReproductiveEvents.DTOs;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Enums;
using AgroLink.Domain.Interfaces;
using MediatR;

namespace AgroLink.Application.Features.ReproductiveEvents.Commands.Create;

public record CreateReproductiveEventCommand(
    int FarmId,
    int AnimalId,
    int UserId,
    CreateReproductiveEventDto Dto
) : IRequest<ReproductiveEventDto>;

public class CreateReproductiveEventCommandHandler(
    IAnimalRepository animalRepository,
    IReproductiveEventRepository reproductiveEventRepository,
    IUnitOfWork unitOfWork
) : IRequestHandler<CreateReproductiveEventCommand, ReproductiveEventDto>
{
    private const int BovineGestationDays = 283;

    public async Task<ReproductiveEventDto> Handle(
        CreateReproductiveEventCommand request,
        CancellationToken cancellationToken
    )
    {
        var dto = request.Dto;
        var animal =
            await animalRepository.GetByIdInFarmAsync(
                request.AnimalId,
                request.FarmId,
                cancellationToken
            ) ?? throw new NotFoundException("Animal", request.AnimalId);

        if (animal.Sex == Sex.Male)
        {
            throw new ArgumentException("Reproductive events apply to female animals only.");
        }

        if (dto.BullId.HasValue)
        {
            var bull = await animalRepository.GetByIdInFarmAsync(
                dto.BullId.Value,
                request.FarmId,
                cancellationToken
            );

            if (bull is null || bull.Sex != Sex.Male)
            {
                throw new ArgumentException("Bull must be a male animal in the same farm.");
            }
        }

        if (dto.Date.Date > DateTime.UtcNow.Date)
        {
            throw new ArgumentException("Event date cannot be in the future.");
        }

        var status = dto.Status ?? ReproductiveEventStatus.Positive;

        if (
            dto.EstimatedMonths.HasValue
            && (
                dto.EventType != ReproductiveEventType.PregnancyCheck
                || status != ReproductiveEventStatus.Positive
            )
        )
        {
            throw new ArgumentException(
                "EstimatedMonths is only allowed for positive pregnancy checks."
            );
        }

        if (dto.EstimatedMonths is < 1 or > 9)
        {
            throw new ArgumentException("EstimatedMonths must be between 1 and 9.");
        }

        DateTime? expectedDueDate = (dto.EventType, status) switch
        {
            (ReproductiveEventType.Mating, ReproductiveEventStatus.Positive) => dto.Date.AddDays(
                BovineGestationDays
            ),
            (ReproductiveEventType.PregnancyCheck, ReproductiveEventStatus.Positive)
                when dto.EstimatedMonths.HasValue => DateTime.UtcNow.Date.AddDays(
                (9 - dto.EstimatedMonths.Value) * 30
            ),
            _ => null,
        };

        if (dto.EventType == ReproductiveEventType.PregnancyCheck)
        {
            var latest = await reproductiveEventRepository.GetLatestPositivePregnancyOrMatingAsync(
                request.AnimalId,
                cancellationToken
            );

            if (status == ReproductiveEventStatus.Positive)
            {
                animal.ReproductiveStatus = ReproductiveStatus.Pregnant;
                animal.UpdatedAt = DateTime.UtcNow;
            }
            else if (latest is null || dto.Date >= latest.Date)
            {
                animal.ReproductiveStatus = ReproductiveStatus.Open;
                animal.UpdatedAt = DateTime.UtcNow;
            }
        }

        var reproductiveEvent = new ReproductiveEvent
        {
            AnimalId = request.AnimalId,
            EventType = dto.EventType,
            Date = dto.Date,
            BullId = dto.BullId,
            Status = status,
            EstimatedMonths = dto.EstimatedMonths,
            ExpectedDueDate = expectedDueDate,
            CreatedByUserId = request.UserId,
            CreatedAt = DateTime.UtcNow,
        };

        await reproductiveEventRepository.AddAsync(reproductiveEvent, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new ReproductiveEventDto
        {
            Id = reproductiveEvent.Id,
            AnimalId = reproductiveEvent.AnimalId,
            EventType = reproductiveEvent.EventType,
            Date = reproductiveEvent.Date,
            BullId = reproductiveEvent.BullId,
            Status = reproductiveEvent.Status,
            EstimatedMonths = reproductiveEvent.EstimatedMonths,
            ExpectedDueDate = reproductiveEvent.ExpectedDueDate,
            CreatedByUserId = reproductiveEvent.CreatedByUserId,
            CreatedAt = reproductiveEvent.CreatedAt,
        };
    }
}
