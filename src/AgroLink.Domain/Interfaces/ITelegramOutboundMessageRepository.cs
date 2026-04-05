using AgroLink.Domain.Entities;

namespace AgroLink.Domain.Interfaces;

public interface ITelegramOutboundMessageRepository : IRepository<TelegramOutboundMessage>
{
    Task<TelegramOutboundMessage?> GetByIdempotencyKeyAsync(
        string idempotencyKey,
        CancellationToken ct = default
    );
}
