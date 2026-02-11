using AgroLink.Application.Interfaces;
using MediatR;

// For IPhotoRepository, IStorageService

namespace AgroLink.Application.Features.Photos.Commands.DeletePhoto;

public class DeletePhotoCommandHandler(
    IPhotoRepository photoRepository,
    IStorageService storageService
) : IRequestHandler<DeletePhotoCommand, Unit>
{
    public async Task<Unit> Handle(DeletePhotoCommand request, CancellationToken cancellationToken)
    {
        var photo = await photoRepository.GetPhotoByIdAsync(request.Id);
        if (photo == null)
        {
            throw new ArgumentException("Photo not found");
        }

        // Try to delete from S3
        if (!string.IsNullOrEmpty(photo.UriRemote))
        {
            try
            {
                var key = ExtractKeyFromUrl(photo.UriRemote);
                await storageService.DeleteFileAsync(key);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to delete photo from S3: {ex.Message}");
            }
        }

        await photoRepository.DeletePhotoAsync(photo);

        return Unit.Value;
    }

    private static string ExtractKeyFromUrl(string url)
    {
        var uri = new Uri(url);
        return uri.AbsolutePath.TrimStart('/');
    }
}
