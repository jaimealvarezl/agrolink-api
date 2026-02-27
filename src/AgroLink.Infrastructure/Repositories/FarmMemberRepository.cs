using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using AgroLink.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AgroLink.Infrastructure.Repositories;

public class FarmMemberRepository(AgroLinkDbContext context)
    : Repository<FarmMember>(context),
        IFarmMemberRepository
{
    public async Task<FarmMember?> GetByFarmAndUserAsync(
        int farmId,
        int userId,
        bool includeUser = false
    )
    {
        var query = _context.FarmMembers.AsQueryable();

        if (includeUser)
        {
            query = query.Include(fm => fm.User);
        }

        return await query.FirstOrDefaultAsync(fm => fm.FarmId == farmId && fm.UserId == userId);
    }

    public async Task<IEnumerable<FarmMember>> GetByFarmIdWithUserAsync(int farmId)
    {
        return await _context
            .FarmMembers.Include(fm => fm.User)
            .Where(fm => fm.FarmId == farmId)
            .ToListAsync();
    }
}
