using AgroLink.Application.Features.Lots.DTOs;
using AgroLink.Domain.Interfaces;
using MediatR;

namespace AgroLink.Application.Features.Lots.Queries.GetById;

public record GetLotByIdQuery(int Id) : IRequest<LotDto?>;

public class GetLotByIdQueryHandler(
    ILotRepository lotRepository,
    IPaddockRepository paddockRepository
) : IRequestHandler<GetLotByIdQuery, LotDto?>
{
    public async Task<LotDto?> Handle(GetLotByIdQuery request, CancellationToken cancellationToken)
    {
        var lot = await lotRepository.GetByIdAsync(request.Id);
        if (lot == null)
        {
            return null;
        }

        var paddock = await paddockRepository.GetByIdAsync(lot.PaddockId);

        return new LotDto
        {
            Id = lot.Id,
            Name = lot.Name,
            PaddockId = lot.PaddockId,
            PaddockName = paddock?.Name ?? "",
            Status = lot.Status,
            CreatedAt = lot.CreatedAt,
        };
    }
}
