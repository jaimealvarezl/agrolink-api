using AgroLink.Domain.Entities;

namespace AgroLink.Domain.Interfaces;

public interface IFarmMemberRepository : IRepository<FarmMember>
{
    Task<FarmMember?> GetByFarmAndUserAsync(
        int farmId,
        int userId,
        bool includeUser = false,
        CancellationToken cancellationToken = default
    );

    Task<IEnumerable<FarmMember>> GetByFarmIdWithUserAsync(
        int farmId,
        CancellationToken cancellationToken = default
    );
}
