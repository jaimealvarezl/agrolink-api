using AgroLink.Application.Common.Exceptions;
using AgroLink.Application.Features.ReproductiveEvents.DTOs;
using AgroLink.Domain.Interfaces;
using MediatR;

namespace AgroLink.Application.Features.ReproductiveEvents.Queries.GetByAnimal;

public record GetReproductiveEventsByAnimalQuery(int AnimalId, int FarmId)
    : IRequest<IReadOnlyList<ReproductiveEventDto>>;

public class GetReproductiveEventsByAnimalQueryHandler(
    IAnimalRepository animalRepository,
    IReproductiveEventRepository reproductiveEventRepository
) : IRequestHandler<GetReproductiveEventsByAnimalQuery, IReadOnlyList<ReproductiveEventDto>>
{
    public async Task<IReadOnlyList<ReproductiveEventDto>> Handle(
        GetReproductiveEventsByAnimalQuery request,
        CancellationToken cancellationToken
    )
    {
        _ =
            await animalRepository.GetByIdInFarmAsync(
                request.AnimalId,
                request.FarmId,
                cancellationToken
            ) ?? throw new NotFoundException("Animal", request.AnimalId);

        var events = await reproductiveEventRepository.GetByAnimalIdAsync(
            request.AnimalId,
            cancellationToken
        );

        return events
            .Select(e => new ReproductiveEventDto
            {
                Id = e.Id,
                AnimalId = e.AnimalId,
                EventType = e.EventType,
                Date = e.Date,
                BullId = e.BullId,
                Status = e.Status,
                EstimatedMonths = e.EstimatedMonths,
                ExpectedDueDate = e.ExpectedDueDate,
                CreatedByUserId = e.CreatedByUserId,
                CreatedAt = e.CreatedAt,
            })
            .ToList();
    }
}
