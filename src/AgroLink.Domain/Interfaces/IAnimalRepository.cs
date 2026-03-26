using AgroLink.Domain.Entities;
using AgroLink.Domain.Enums;

namespace AgroLink.Domain.Interfaces;

public interface IAnimalRepository : IRepository<Animal>
{
    Task<IEnumerable<Animal>> GetByLotIdAsync(int lotId, int userId);
    Task<Animal?> GetByIdAsync(int id, int userId);
    Task<Animal?> GetAnimalWithOwnersAsync(int id);
    Task<Animal?> GetAnimalWithGenealogyAsync(int id);
    Task<IEnumerable<Animal>> GetChildrenAsync(int parentId);
    Task<Animal?> GetByCuiaAsync(string cuia);
    Task<bool> IsCuiaUniqueInFarmAsync(string cuia, int farmId, int? excludeAnimalId = null);
    Task<bool> IsNameUniqueInFarmAsync(string name, int farmId, int? excludeAnimalId = null);

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
        bool includeRetired = false
    );

    Task<IEnumerable<Animal>> GetAllByFarmAsync(
        int farmId,
        CancellationToken cancellationToken = default
    );

    Task<Animal?> GetAnimalDetailsAsync(int id, int userId);
    Task<Animal?> GetLotWithPaddockAsync(int id);
    Task<Animal?> GetByIdInFarmAsync(int id, int farmId);

    Task<List<string>> GetDistinctColorsAsync(
        int farmId,
        CancellationToken cancellationToken = default
    );

    Task<List<string>> GetDistinctBreedsAsync(
        int farmId,
        CancellationToken cancellationToken = default
    );
}
