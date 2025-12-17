using AgroLink.Application.DTOs;
using AgroLink.Domain.Interfaces;
using MediatR;

namespace AgroLink.Application.Features.Paddocks.Queries.GetById;

public record GetPaddockByIdQuery(int Id) : IRequest<PaddockDto?>;

public class GetPaddockByIdQueryHandler(
    IPaddockRepository paddockRepository,
    IFarmRepository farmRepository
) : IRequestHandler<GetPaddockByIdQuery, PaddockDto?>
{
    public async Task<PaddockDto?> Handle(
        GetPaddockByIdQuery request,
        CancellationToken cancellationToken
    )
    {
        var paddock = await paddockRepository.GetByIdAsync(request.Id);
        if (paddock == null)
        {
            return null;
        }

        var farm = await farmRepository.GetByIdAsync(paddock.FarmId);

        return new PaddockDto
        {
            Id = paddock.Id,
            Name = paddock.Name,
            FarmId = paddock.FarmId,
            FarmName = farm?.Name ?? "",
            CreatedAt = paddock.CreatedAt,
        };
    }
}
