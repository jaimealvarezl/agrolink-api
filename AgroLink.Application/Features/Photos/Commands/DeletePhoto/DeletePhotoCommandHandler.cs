using AgroLink.Application.Interfaces;
using MediatR;

// For IPhotoRepository, IAwsS3Service

namespace AgroLink.Application.Features.Photos.Commands.DeletePhoto;

public class DeletePhotoCommandHandler(IPhotoRepository photoRepository, IAwsS3Service awsS3Service)
    : IRequestHandler<DeletePhotoCommand, Unit>
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
                await awsS3Service.DeleteFileAsync(key);
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
