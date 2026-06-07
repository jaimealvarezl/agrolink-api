using AgroLink.Application.Interfaces;
using AgroLink.Domain.Entities;
using AgroLink.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AgroLink.Infrastructure.Repositories;

public class DeviceTokenRepository(AgroLinkDbContext context) : IDeviceTokenRepository
{
    public async Task UpsertAsync(DeviceToken token, CancellationToken ct)
    {
        var existing = await context.DeviceTokens.FirstOrDefaultAsync(
            t => t.Token == token.Token,
            ct
        );
        if (existing != null)
        {
            existing.LastSeenAt = DateTime.UtcNow;
            return;
        }

        await context.DeviceTokens.AddAsync(token, ct);
    }

    public async Task<IReadOnlyList<string>> GetTokensByFarmAsync(int farmId, CancellationToken ct)
    {
        return await context
            .DeviceTokens.AsNoTracking()
            .Where(t =>
                context.FarmMembers.Any(m => m.FarmId == farmId && m.UserId == t.UserId)
                || context.Farms.Any(f =>
                    f.Id == farmId
                    && f.Owner != null
                    && f.Owner.UserId.HasValue
                    && f.Owner.UserId.Value == t.UserId
                )
            )
            .Select(t => t.Token)
            .Distinct()
            .ToListAsync(ct);
    }

    public async Task DeleteAsync(string token, int userId, CancellationToken ct)
    {
        var query = context.DeviceTokens.Where(t => t.Token == token);
        if (userId > 0)
        {
            query = query.Where(t => t.UserId == userId);
        }

        var existing = await query.FirstOrDefaultAsync(ct);
        if (existing is null)
        {
            return;
        }

        context.DeviceTokens.Remove(existing);
    }
}
