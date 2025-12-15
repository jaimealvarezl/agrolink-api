using AgroLink.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AgroLink.Application.Features.Photos.Commands.SyncPendingPhotos;

public class SyncPendingPhotosCommandHandler(AgroLinkDbContext context)
    : IRequestHandler<SyncPendingPhotosCommand, Unit>
{
    public async Task<Unit> Handle(
        SyncPendingPhotosCommand request,
        CancellationToken cancellationToken
    )
    {
        var pendingPhotos = await context
            .Photos.Where(p => !p.Uploaded)
            .ToListAsync(cancellationToken);

        foreach (var photo in pendingPhotos)
        {
            try
            {
                // This would need to read the local file and upload it
                // Implementation depends on how local files are stored
                // For now, we'll just mark as attempted
                photo.UpdatedAt = DateTime.UtcNow;
                context.Photos.Update(photo);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to sync photo {photo.Id}: {ex.Message}");
            }
        }

        await context.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
