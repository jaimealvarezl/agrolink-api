using AgroLink.Domain.Entities;

namespace AgroLink.Domain.Interfaces;

public interface IFarmMemberRepository : IRepository<FarmMember>
{
    Task<FarmMember?> GetByFarmAndUserAsync(int farmId, int userId, bool includeUser = false);
    Task<IEnumerable<FarmMember>> GetByFarmIdWithUserAsync(int farmId);
}
