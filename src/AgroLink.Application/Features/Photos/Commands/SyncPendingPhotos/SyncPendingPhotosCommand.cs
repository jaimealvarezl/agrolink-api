using MediatR;

namespace AgroLink.Application.Features.Photos.Commands.SyncPendingPhotos;

public record SyncPendingPhotosCommand() : IRequest<Unit>;
