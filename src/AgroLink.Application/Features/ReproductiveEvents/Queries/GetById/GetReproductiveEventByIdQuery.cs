using AgroLink.Application.Common.Exceptions;
using AgroLink.Application.Features.ReproductiveEvents.DTOs;
using AgroLink.Domain.Interfaces;
using MediatR;

namespace AgroLink.Application.Features.ReproductiveEvents.Queries.GetById;

public record GetReproductiveEventByIdQuery(int Id, int AnimalId, int FarmId)
    : IRequest<ReproductiveEventDto?>;

public class GetReproductiveEventByIdQueryHandler(
    IAnimalRepository animalRepository,
    IReproductiveEventRepository reproductiveEventRepository
) : IRequestHandler<GetReproductiveEventByIdQuery, ReproductiveEventDto?>
{
    public async Task<ReproductiveEventDto?> Handle(
        GetReproductiveEventByIdQuery request,
        CancellationToken cancellationToken
    )
    {
        _ =
            await animalRepository.GetByIdInFarmAsync(
                request.AnimalId,
                request.FarmId,
                cancellationToken
            ) ?? throw new NotFoundException("Animal", request.AnimalId);

        var reproductiveEvent = await reproductiveEventRepository.GetByIdAsync(
            request.Id,
            cancellationToken
        );

        if (reproductiveEvent is null || reproductiveEvent.AnimalId != request.AnimalId)
        {
            return null;
        }

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
