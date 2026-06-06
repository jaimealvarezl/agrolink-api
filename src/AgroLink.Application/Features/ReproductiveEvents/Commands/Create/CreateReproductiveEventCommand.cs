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
        var eventDate = dto.Date.Kind switch
        {
            DateTimeKind.Utc => dto.Date,
            DateTimeKind.Local => dto.Date.ToUniversalTime(),
            _ => DateTime.SpecifyKind(dto.Date, DateTimeKind.Utc),
        };
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

        if (eventDate.Date > DateTime.UtcNow.Date)
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
            (ReproductiveEventType.Mating, ReproductiveEventStatus.Positive) => eventDate.AddDays(
                BovineGestationDays
            ),
            (ReproductiveEventType.PregnancyCheck, ReproductiveEventStatus.Positive)
                when dto.EstimatedMonths.HasValue => eventDate.AddDays(
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
                if (animal.ReproductiveStatus != ReproductiveStatus.Pregnant)
                {
                    animal.ReproductiveStatus = ReproductiveStatus.Pregnant;
                    animal.UpdatedAt = DateTime.UtcNow;
                }
            }
            else if (latest is null || eventDate >= latest.Date)
            {
                if (animal.ReproductiveStatus != ReproductiveStatus.Open)
                {
                    animal.ReproductiveStatus = ReproductiveStatus.Open;
                    animal.UpdatedAt = DateTime.UtcNow;
                }
            }
        }

        var reproductiveEvent = new ReproductiveEvent
        {
            AnimalId = request.AnimalId,
            EventType = dto.EventType,
            Date = eventDate,
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
