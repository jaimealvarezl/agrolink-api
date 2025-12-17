using AgroLink.Application.Features.Paddocks.DTOs;
using AgroLink.Domain.Interfaces;
using MediatR;

namespace AgroLink.Application.Features.Paddocks.Queries.GetAll;

public record GetAllPaddocksQuery : IRequest<IEnumerable<PaddockDto>>;

public class GetAllPaddocksQueryHandler(
    IPaddockRepository paddockRepository,
    IFarmRepository farmRepository
) : IRequestHandler<GetAllPaddocksQuery, IEnumerable<PaddockDto>>
{
    public async Task<IEnumerable<PaddockDto>> Handle(
        GetAllPaddocksQuery request,
        CancellationToken cancellationToken
    )
    {
        var paddocks = await paddockRepository.GetAllAsync();
        var result = new List<PaddockDto>();

        foreach (var paddock in paddocks)
        {
            var farm = await farmRepository.GetByIdAsync(paddock.FarmId);
            result.Add(
                new PaddockDto
                {
                    Id = paddock.Id,
                    Name = paddock.Name,
                    FarmId = paddock.FarmId,
                    FarmName = farm?.Name ?? "",
                    CreatedAt = paddock.CreatedAt,
                }
            );
        }

        return result;
    }
}
