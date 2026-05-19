using AgroLink.Application.Features.HerdComposition.DTOs;
using AgroLink.Application.Interfaces;
using AgroLink.Domain.Enums;
using MediatR;

namespace AgroLink.Application.Features.HerdComposition.Queries.GetHerdComposition;

public class GetHerdCompositionQueryHandler(IHerdCompositionRepository repository)
    : IRequestHandler<GetHerdCompositionQuery, HerdCompositionDto>
{
    public async Task<HerdCompositionDto> Handle(
        GetHerdCompositionQuery request,
        CancellationToken cancellationToken
    )
    {
        var farmId = request.FarmId;

        // Round trip 1: grouped by lot + sex — feeds both byLot and byLotAndSex
        var lotSexRows = await repository.GetLotSexGroupsAsync(farmId, cancellationToken);

        var byLot = lotSexRows
            .GroupBy(r => new { r.LotId, r.LotName })
            .Select(g => new LotCountDto
            {
                LotId = g.Key.LotId,
                LotName = g.Key.LotName,
                AnimalCount = g.Sum(r => r.Count),
            })
            .OrderBy(x => x.LotName)
            .ToList();

        var byLotAndSex = lotSexRows
            .GroupBy(r => new { r.LotId, r.LotName })
            .Select(g => new LotSexCountDto
            {
                LotId = g.Key.LotId,
                LotName = g.Key.LotName,
                MaleCount = g.Where(r => r.Sex == Sex.Male).Sum(r => r.Count),
                FemaleCount = g.Where(r => r.Sex == Sex.Female).Sum(r => r.Count),
            })
            .OrderBy(x => x.LotName)
            .ToList();

        // Round trip 2: animals with their owner name sets
        var animalOwnerRows = await repository.GetAnimalOwnerRowsAsync(farmId, cancellationToken);

        var byOwnerGroup = animalOwnerRows
            .GroupBy(r => string.Join('\x1F', r.OwnerNames.OrderBy(n => n, StringComparer.Ordinal)))
            .Select(g => new OwnerGroupDto
            {
                OwnerNames = g.First().OwnerNames.OrderBy(n => n, StringComparer.Ordinal).ToList(),
                AnimalCount = g.Count(),
            })
            .OrderBy(x => x.OwnerNames.Count == 0 ? 1 : 0)
            .ThenBy(x => string.Join('\x1F', x.OwnerNames))
            .ToList();

        return new HerdCompositionDto
        {
            ByOwnerGroup = byOwnerGroup,
            ByLot = byLot,
            ByLotAndSex = byLotAndSex,
        };
    }
}
