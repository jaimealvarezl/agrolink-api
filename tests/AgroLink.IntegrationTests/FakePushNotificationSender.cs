using AgroLink.Application.Features.Notifications.DTOs;
using AgroLink.Application.Interfaces;

namespace AgroLink.IntegrationTests;

public class FakePushNotificationSender : IPushNotificationSender
{
    private readonly List<PushSendCall> _calls = [];

    public IReadOnlyList<PushSendCall> Calls => _calls;

    public Task<PushSendResult> SendAsync(
        IReadOnlyList<string> fcmTokens,
        string title,
        string body,
        IDictionary<string, string> data,
        CancellationToken ct
    )
    {
        _calls.Add(
            new PushSendCall(fcmTokens.ToArray(), title, body, new Dictionary<string, string>(data))
        );
        return Task.FromResult(new PushSendResult(fcmTokens.Count, []));
    }
}

public record PushSendCall(
    string[] Tokens,
    string Title,
    string Body,
    IDictionary<string, string> Data
);
