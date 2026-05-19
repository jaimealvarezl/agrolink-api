using AgroLink.Domain.Enums;

namespace AgroLink.Application.Interfaces;

public record LotSexRow(int LotId, string LotName, Sex Sex, int Count);

public record AnimalOwnerRow(int AnimalId, List<string> OwnerNames);

public interface IHerdCompositionRepository
{
    Task<List<LotSexRow>> GetLotSexGroupsAsync(int farmId, CancellationToken ct = default);
    Task<List<AnimalOwnerRow>> GetAnimalOwnerRowsAsync(int farmId, CancellationToken ct = default);
}
