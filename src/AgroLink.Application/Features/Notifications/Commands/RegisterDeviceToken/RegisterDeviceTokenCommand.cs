using AgroLink.Application.Features.Notifications.DTOs;
using AgroLink.Application.Interfaces;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using MediatR;

namespace AgroLink.Application.Features.Notifications.Commands.RegisterDeviceToken;

public record RegisterDeviceTokenCommand(string Token, string Platform, int UserId)
    : IRequest<DeviceTokenDto>;

public class RegisterDeviceTokenCommandHandler(
    IDeviceTokenRepository deviceTokenRepository,
    IUnitOfWork unitOfWork
) : IRequestHandler<RegisterDeviceTokenCommand, DeviceTokenDto>
{
    public async Task<DeviceTokenDto> Handle(
        RegisterDeviceTokenCommand request,
        CancellationToken cancellationToken
    )
    {
        if (string.IsNullOrWhiteSpace(request.Token))
        {
            throw new ArgumentException("Token is required.");
        }

        if (request.Token.Length > 512)
        {
            throw new ArgumentException("Token must be at most 512 characters.");
        }

        var normalizedPlatform = request.Platform.Trim().ToLowerInvariant();
        if (normalizedPlatform is not ("ios" or "android"))
        {
            throw new ArgumentException("Platform must be ios or android.");
        }

        var now = DateTime.UtcNow;

        await deviceTokenRepository.UpsertAsync(
            new DeviceToken
            {
                UserId = request.UserId,
                Token = request.Token.Trim(),
                Platform = normalizedPlatform,
                CreatedAt = now,
                LastSeenAt = now,
            },
            cancellationToken
        );

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new DeviceTokenDto(request.UserId, normalizedPlatform, now);
    }
}
