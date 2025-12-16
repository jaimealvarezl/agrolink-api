using AgroLink.Application.Interfaces;
using MediatR;

// For IPhotoRepository

namespace AgroLink.Application.Features.Photos.Commands.SyncPendingPhotos;

public class SyncPendingPhotosCommandHandler(IPhotoRepository photoRepository)
    : IRequestHandler<SyncPendingPhotosCommand, Unit>
{
    public async Task<Unit> Handle(
        SyncPendingPhotosCommand request,
        CancellationToken cancellationToken
    )
    {
        var pendingPhotos = await photoRepository.GetPendingPhotosAsync();

        foreach (var photo in pendingPhotos)
        {
            try
            {
                // This would need to read the local file and upload it
                // Implementation depends on how local files are stored
                // For now, we'll just mark as attempted
                photo.UpdatedAt = DateTime.UtcNow;
                await photoRepository.UpdatePhotoAsync(photo);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to sync photo {photo.Id}: {ex.Message}");
            }
        }

        return Unit.Value;
    }
}
