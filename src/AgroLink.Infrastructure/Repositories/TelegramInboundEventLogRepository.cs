using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using AgroLink.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AgroLink.Infrastructure.Repositories;

public class TelegramInboundEventLogRepository(AgroLinkDbContext context)
    : Repository<TelegramInboundEventLog>(context),
        ITelegramInboundEventLogRepository
{
    public async Task<TelegramInboundEventLog?> GetByTelegramUpdateIdAsync(
        long telegramUpdateId,
        CancellationToken ct = default
    )
    {
        return await _dbSet.FirstOrDefaultAsync(x => x.TelegramUpdateId == telegramUpdateId, ct);
    }
}
