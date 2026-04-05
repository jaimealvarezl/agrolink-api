using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using AgroLink.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AgroLink.Infrastructure.Repositories;

public class TelegramOutboundMessageRepository(AgroLinkDbContext context)
    : Repository<TelegramOutboundMessage>(context),
        ITelegramOutboundMessageRepository
{
    public async Task<TelegramOutboundMessage?> GetByIdempotencyKeyAsync(
        string idempotencyKey,
        CancellationToken ct = default
    )
    {
        return await _dbSet.FirstOrDefaultAsync(x => x.IdempotencyKey == idempotencyKey, ct);
    }
}
