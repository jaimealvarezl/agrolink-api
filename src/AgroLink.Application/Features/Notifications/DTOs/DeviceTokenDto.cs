namespace AgroLink.Application.Features.Notifications.DTOs;

public record DeviceTokenDto(int UserId, string Platform, DateTime LastSeenAt);
