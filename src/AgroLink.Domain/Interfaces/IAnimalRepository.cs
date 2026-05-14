using AgroLink.Domain.Entities;
using AgroLink.Domain.Enums;

namespace AgroLink.Domain.Interfaces;

public interface IAnimalRepository : IRepository<Animal>
{
    Task<IEnumerable<Animal>> GetByLotIdAsync(
        int lotId,
        int userId,
        CancellationToken cancellationToken = default
    );

    Task<Animal?> GetByIdAsync(int id, int userId, CancellationToken cancellationToken = default);
    Task<Animal?> GetByEarTagInFarmAsync(int farmId, string earTag, CancellationToken ct = default);

    Task<Animal?> FindByReferenceInFarmAsync(
        int farmId,
        string reference,
        CancellationToken ct = default
    );

    Task<IEnumerable<Animal>> GetChildrenAsync(
        int parentId,
        CancellationToken cancellationToken = default
    );

    Task<Animal?> GetByCuiaAsync(string cuia, CancellationToken cancellationToken = default);

    Task<bool> IsCuiaUniqueInFarmAsync(
        string cuia,
        int farmId,
        int? excludeAnimalId = null,
        CancellationToken cancellationToken = default
    );

    Task<bool> IsNameUniqueInFarmAsync(
        string name,
        int farmId,
        int? excludeAnimalId = null,
        CancellationToken cancellationToken = default
    );

    Task<(IEnumerable<Animal> Items, int TotalCount)> GetPagedListAsync(
        int farmId,
        int page,
        int pageSize,
        int? lotId = null,
        string? searchTerm = null,
        bool isSick = false,
        bool isPregnant = false,
        bool isMissing = false,
        Sex? sex = null,
        bool includeRetired = false,
        CancellationToken cancellationToken = default
    );

    Task<IEnumerable<Animal>> GetAllByFarmAsync(
        int farmId,
        CancellationToken cancellationToken = default
    );

    Task<Animal?> GetAnimalDetailsAsync(
        int id,
        int userId,
        CancellationToken cancellationToken = default
    );

    Task<Animal?> GetLotWithPaddockAsync(int id, CancellationToken cancellationToken = default);
    Task<Animal?> GetByIdInFarmAsync(int id, int farmId, CancellationToken ct = default);

    Task<List<string>> GetDistinctColorsAsync(
        int farmId,
        CancellationToken cancellationToken = default
    );

    Task<List<string>> GetDistinctBreedsAsync(
        int farmId,
        CancellationToken cancellationToken = default
    );
}
