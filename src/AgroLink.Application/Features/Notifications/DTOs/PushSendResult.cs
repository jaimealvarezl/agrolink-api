namespace AgroLink.Application.Features.Notifications.DTOs;

public record PushSendResult(int SuccessCount, string[] UnregisteredTokens);
