using AgroLink.Application.Features.Lots.DTOs;
using AgroLink.Application.Interfaces;
using AgroLink.Domain.Interfaces;
using MediatR;

namespace AgroLink.Application.Features.Lots.Queries.GetById;

public record GetLotByIdQuery(int Id) : IRequest<LotDto?>;

public class GetLotByIdQueryHandler(
    ILotRepository lotRepository,
    IPaddockRepository paddockRepository,
    ICurrentUserService currentUserService
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

        // Security check: ensure lot belongs to the current farm context
        if (
            currentUserService.CurrentFarmId.HasValue
            && paddock != null
            && paddock.FarmId != currentUserService.CurrentFarmId.Value
        )
        {
            return null;
        }

        return new LotDto
        {
            Id = lot.Id,
            Name = lot.Name,
            PaddockId = lot.PaddockId,
            FarmId = paddock?.FarmId ?? 0,
            PaddockName = paddock?.Name ?? "",
            Status = lot.Status,
            AnimalCount = lot.Animals?.Count ?? 0,
            CreatedAt = lot.CreatedAt,
        };
    }
}
