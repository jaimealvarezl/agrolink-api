using AgroLink.Domain.Entities;
using AgroLink.Domain.Enums;

namespace AgroLink.Application.Interfaces;

public interface ISentNotificationRepository
{
    Task<bool> ExistsAsync(
        int animalId,
        NotificationType type,
        DateOnly dueDate,
        CancellationToken ct
    );

    Task AddAsync(SentNotification notification, CancellationToken ct);
}
