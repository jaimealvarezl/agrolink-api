using AgroLink.Application.Interfaces;
using AgroLink.Domain.Interfaces;
using MediatR;

namespace AgroLink.Application.Features.Notifications.Commands.DeleteDeviceToken;

public record DeleteDeviceTokenCommand(string Token, int UserId) : IRequest;

public class DeleteDeviceTokenCommandHandler(
    IDeviceTokenRepository deviceTokenRepository,
    IUnitOfWork unitOfWork
) : IRequestHandler<DeleteDeviceTokenCommand>
{
    public async Task Handle(DeleteDeviceTokenCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Token))
        {
            return;
        }

        await deviceTokenRepository.DeleteAsync(
            request.Token.Trim(),
            request.UserId,
            cancellationToken
        );
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
