using AgroLink.Domain.Entities;

namespace AgroLink.Domain.Interfaces;

public interface ITelegramInboundEventLogRepository : IRepository<TelegramInboundEventLog>
{
    Task<TelegramInboundEventLog?> GetByTelegramUpdateIdAsync(
        long telegramUpdateId,
        CancellationToken ct = default
    );
}
