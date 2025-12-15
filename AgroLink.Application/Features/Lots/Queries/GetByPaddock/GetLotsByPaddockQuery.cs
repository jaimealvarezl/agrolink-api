using AgroLink.Application.DTOs;
using AgroLink.Domain.Interfaces;
using MediatR;

namespace AgroLink.Application.Features.Lots.Queries.GetByPaddock;

public record GetLotsByPaddockQuery(int PaddockId) : IRequest<IEnumerable<LotDto>>;

public class GetLotsByPaddockQueryHandler(
    ILotRepository lotRepository,
    IPaddockRepository paddockRepository
) : IRequestHandler<GetLotsByPaddockQuery, IEnumerable<LotDto>>
{
    public async Task<IEnumerable<LotDto>> Handle(
        GetLotsByPaddockQuery request,
        CancellationToken cancellationToken
    )
    {
        var lots = await lotRepository.GetByPaddockIdAsync(request.PaddockId);
        var paddock = await paddockRepository.GetByIdAsync(request.PaddockId);

        return lots.Select(l => new LotDto
        {
            Id = l.Id,
            Name = l.Name,
            PaddockId = l.PaddockId,
            PaddockName = paddock?.Name ?? "",
            Status = l.Status,
            CreatedAt = l.CreatedAt,
        });
    }
}
