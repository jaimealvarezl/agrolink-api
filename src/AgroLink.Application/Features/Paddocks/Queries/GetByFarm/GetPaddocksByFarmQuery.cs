using AgroLink.Application.Features.Paddocks.DTOs;
using AgroLink.Domain.Interfaces;
using MediatR;

namespace AgroLink.Application.Features.Paddocks.Queries.GetByFarm;

public record GetPaddocksByFarmQuery(int FarmId) : IRequest<IEnumerable<PaddockDto>>;

public class GetPaddocksByFarmQueryHandler(
    IPaddockRepository paddockRepository,
    IFarmRepository farmRepository
) : IRequestHandler<GetPaddocksByFarmQuery, IEnumerable<PaddockDto>>
{
    public async Task<IEnumerable<PaddockDto>> Handle(
        GetPaddocksByFarmQuery request,
        CancellationToken cancellationToken
    )
    {
        var paddocks = await paddockRepository.GetByFarmIdAsync(request.FarmId);
        var farm = await farmRepository.GetByIdAsync(request.FarmId);

        return paddocks.Select(p => new PaddockDto
        {
            Id = p.Id,
            Name = p.Name,
            FarmId = p.FarmId,
            FarmName = farm?.Name ?? "",
            CreatedAt = p.CreatedAt,
        });
    }
}
