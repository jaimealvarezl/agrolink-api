using AgroLink.Domain.Entities;

namespace AgroLink.Domain.Interfaces;

public interface IFarmMemberRepository : IRepository<FarmMember>
{
    Task<FarmMember?> GetByFarmAndUserAsync(int farmId, int userId);
    Task<IEnumerable<FarmMember>> GetByFarmIdWithUserAsync(int farmId);
}
