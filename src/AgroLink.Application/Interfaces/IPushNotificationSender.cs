using AgroLink.Application.Features.Notifications.DTOs;

namespace AgroLink.Application.Interfaces;

public interface IPushNotificationSender
{
    Task<PushSendResult> SendAsync(
        IReadOnlyList<string> fcmTokens,
        string title,
        string body,
        IDictionary<string, string> data,
        CancellationToken ct
    );
}
