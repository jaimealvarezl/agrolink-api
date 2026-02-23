using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using AgroLink.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AgroLink.Infrastructure.Repositories;

public class FarmMemberRepository : Repository<FarmMember>, IFarmMemberRepository
{
    public FarmMemberRepository(AgroLinkDbContext context)
        : base(context) { }

    public async Task<FarmMember?> GetByFarmAndUserAsync(int farmId, int userId)
    {
        return await _context.FarmMembers.FirstOrDefaultAsync(fm =>
            fm.FarmId == farmId && fm.UserId == userId
        );
    }
}
