using AgroLink.Application.Features.Notifications.DTOs;
using AgroLink.Application.Interfaces;
using FirebaseAdmin.Messaging;

namespace AgroLink.Infrastructure.Services;

public class FcmPushNotificationSender : IPushNotificationSender
{
    public async Task<PushSendResult> SendAsync(
        IReadOnlyList<string> fcmTokens,
        string title,
        string body,
        IDictionary<string, string> data,
        CancellationToken ct
    )
    {
        if (fcmTokens.Count == 0)
        {
            return new PushSendResult(0, []);
        }

        var message = new MulticastMessage
        {
            Tokens = fcmTokens.ToList(),
            Notification = new Notification { Title = title, Body = body },
            Data = new Dictionary<string, string>(data),
            Android = new AndroidConfig { Priority = Priority.High },
        };

        var batchResponse = await FirebaseMessaging.DefaultInstance.SendEachForMulticastAsync(
            message,
            ct
        );

        var unregisteredTokens = new List<string>();

        for (var i = 0; i < batchResponse.Responses.Count; i++)
        {
            var response = batchResponse.Responses[i];
            var code = response.Exception?.MessagingErrorCode;
            if (code is MessagingErrorCode.Unregistered or MessagingErrorCode.InvalidArgument)
            {
                unregisteredTokens.Add(fcmTokens[i]);
            }
        }

        return new PushSendResult(batchResponse.SuccessCount, unregisteredTokens.ToArray());
    }
}
