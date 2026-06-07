using AgroLink.Application.Interfaces;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Enums;
using AgroLink.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AgroLink.Infrastructure.Repositories;

public class SentNotificationRepository(AgroLinkDbContext context) : ISentNotificationRepository
{
    public async Task<bool> ExistsAsync(
        int animalId,
        NotificationType type,
        DateOnly dueDate,
        CancellationToken ct
    )
    {
        return await context.SentNotifications.AnyAsync(
            n =>
                n.AnimalId == animalId
                && n.NotificationType == type
                && n.ExpectedDueDate == dueDate,
            ct
        );
    }

    public async Task AddAsync(SentNotification notification, CancellationToken ct)
    {
        await context.SentNotifications.AddAsync(notification, ct);
    }
}
