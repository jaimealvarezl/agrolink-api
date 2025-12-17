using AgroLink.Application.DTOs;
using AgroLink.Domain.Interfaces;
using MediatR;

namespace AgroLink.Application.Features.Lots.Queries.GetAll;

public record GetAllLotsQuery : IRequest<IEnumerable<LotDto>>;

public class GetAllLotsQueryHandler(
    ILotRepository lotRepository,
    IPaddockRepository paddockRepository
) : IRequestHandler<GetAllLotsQuery, IEnumerable<LotDto>>
{
    public async Task<IEnumerable<LotDto>> Handle(
        GetAllLotsQuery request,
        CancellationToken cancellationToken
    )
    {
        var lots = await lotRepository.GetAllAsync();
        var result = new List<LotDto>();

        foreach (var lot in lots)
        {
            var paddock = await paddockRepository.GetByIdAsync(lot.PaddockId);
            result.Add(
                new LotDto
                {
                    Id = lot.Id,
                    Name = lot.Name,
                    PaddockId = lot.PaddockId,
                    PaddockName = paddock?.Name ?? "",
                    Status = lot.Status,
                    CreatedAt = lot.CreatedAt,
                }
            );
        }

        return result;
    }
}
