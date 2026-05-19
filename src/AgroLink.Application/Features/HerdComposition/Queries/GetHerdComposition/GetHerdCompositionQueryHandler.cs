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

        var lotGroups = lotSexRows
            .GroupBy(r => new { r.LotId, r.LotName })
            .OrderBy(g => g.Key.LotName)
            .ToList();

        var byLot = lotGroups
            .Select(g => new LotCountDto
            {
                LotId = g.Key.LotId,
                LotName = g.Key.LotName,
                AnimalCount = g.Sum(r => r.Count),
            })
            .ToList();

        var byLotAndSex = lotGroups
            .Select(g => new LotSexCountDto
            {
                LotId = g.Key.LotId,
                LotName = g.Key.LotName,
                MaleCount = g.Where(r => r.Sex == Sex.Male).Sum(r => r.Count),
                FemaleCount = g.Where(r => r.Sex == Sex.Female).Sum(r => r.Count),
            })
            .ToList();

        // Round trip 2: animals with their owner name sets
        var animalOwnerRows = await repository.GetAnimalOwnerRowsAsync(farmId, cancellationToken);

        var byOwnerGroup = animalOwnerRows
            .Select(r => r.OwnerNames.OrderBy(n => n, StringComparer.Ordinal).ToList())
            .GroupBy(names => string.Join('\x1F', names))
            .Select(g => new OwnerGroupDto { OwnerNames = g.First(), AnimalCount = g.Count() })
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
