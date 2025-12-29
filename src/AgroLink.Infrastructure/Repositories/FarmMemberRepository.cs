using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using AgroLink.Infrastructure.Data;

namespace AgroLink.Infrastructure.Repositories;

public class FarmMemberRepository : Repository<FarmMember>, IFarmMemberRepository
{
    public FarmMemberRepository(AgroLinkDbContext context)
        : base(context) { }
}
