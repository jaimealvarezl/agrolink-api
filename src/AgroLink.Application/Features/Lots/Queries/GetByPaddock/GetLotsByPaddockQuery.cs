using AgroLink.Application.Features.Lots.DTOs;
using AgroLink.Application.Interfaces;
using AgroLink.Domain.Interfaces;
using MediatR;

namespace AgroLink.Application.Features.Lots.Queries.GetByPaddock;

public record GetLotsByPaddockQuery(int PaddockId) : IRequest<IEnumerable<LotDto>>;

public class GetLotsByPaddockQueryHandler(
    ILotRepository lotRepository,
    IPaddockRepository paddockRepository,
    ICurrentUserService currentUserService
) : IRequestHandler<GetLotsByPaddockQuery, IEnumerable<LotDto>>
{
    public async Task<IEnumerable<LotDto>> Handle(
        GetLotsByPaddockQuery request,
        CancellationToken cancellationToken
    )
    {
        var paddock = await paddockRepository.GetByIdAsync(request.PaddockId);

        // Security check: ensure paddock belongs to the current farm context
        if (
            currentUserService.CurrentFarmId.HasValue
            && paddock != null
            && paddock.FarmId != currentUserService.CurrentFarmId.Value
        )
        {
            return [];
        }

        var lots = await lotRepository.GetByPaddockIdAsync(request.PaddockId);

        return lots.Select(l => new LotDto
        {
            Id = l.Id,
            Name = l.Name,
            PaddockId = l.PaddockId,
            FarmId = paddock?.FarmId ?? 0,
            PaddockName = paddock?.Name ?? "",
            Status = l.Status,
            AnimalCount = l.Animals?.Count ?? 0,
            CreatedAt = l.CreatedAt,
        });
    }
}
