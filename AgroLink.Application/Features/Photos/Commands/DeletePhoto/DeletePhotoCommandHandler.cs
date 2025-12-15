using AgroLink.Infrastructure.Data;
using Amazon.S3;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace AgroLink.Application.Features.Photos.Commands.DeletePhoto;

public class DeletePhotoCommandHandler(
    AgroLinkDbContext context,
    IAmazonS3 s3Client,
    IConfiguration configuration
) : IRequestHandler<DeletePhotoCommand, Unit>
{
    private readonly string _bucketName = configuration["AWS:S3BucketName"] ?? "agrolink-photos";

    public async Task<Unit> Handle(DeletePhotoCommand request, CancellationToken cancellationToken)
    {
        var photo = await context.Photos.FindAsync(request.Id);
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
                await s3Client.DeleteObjectAsync(_bucketName, key, cancellationToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to delete photo from S3: {ex.Message}");
            }
        }

        context.Photos.Remove(photo);
        await context.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }

    private static string ExtractKeyFromUrl(string url)
    {
        var uri = new Uri(url);
        return uri.AbsolutePath.TrimStart('/');
    }
}
