using AgroLink.Domain.Entities;

namespace AgroLink.Domain.Interfaces;

public interface IAnimalRepository : IRepository<Animal>
{
    Task<IEnumerable<Animal>> GetByLotIdAsync(int lotId);
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
        bool isMissing = false
    );

    Task<Animal?> GetAnimalDetailsAsync(int id);
}
