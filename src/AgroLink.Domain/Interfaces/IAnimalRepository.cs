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
}
