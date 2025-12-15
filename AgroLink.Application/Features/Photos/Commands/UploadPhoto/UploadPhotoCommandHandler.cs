using AgroLink.Application.DTOs;
using AgroLink.Domain.Entities;
using AgroLink.Infrastructure.Data;
using Amazon.S3;
using Amazon.S3.Model;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace AgroLink.Application.Features.Photos.Commands.UploadPhoto;

public class UploadPhotoCommandHandler(
    AgroLinkDbContext context,
    IAmazonS3 s3Client,
    IConfiguration configuration
) : IRequestHandler<UploadPhotoCommand, PhotoDto>
{
    private readonly string _bucketName = configuration["AWS:S3BucketName"] ?? "agrolink-photos";

    public async Task<PhotoDto> Handle(
        UploadPhotoCommand request,
        CancellationToken cancellationToken
    )
    {
        var photo = new Photo
        {
            EntityType = request.PhotoDto.EntityType,
            EntityId = request.PhotoDto.EntityId,
            UriLocal =
                $"local/{request.PhotoDto.EntityType.ToLower()}/{request.PhotoDto.EntityId}/{request.FileName}",
            Description = request.PhotoDto.Description,
        };

        context.Photos.Add(photo);
        await context.SaveChangesAsync(cancellationToken);

        // Try to upload to S3
        try
        {
            var key =
                $"photos/{request.PhotoDto.EntityType.ToLower()}/{request.PhotoDto.EntityId}/{photo.Id}_{request.FileName}";

            var putRequest = new PutObjectRequest
            {
                BucketName = _bucketName,
                Key = key,
                InputStream = request.FileStream,
                ContentType = GetContentType(request.FileName),
            };

            await s3Client.PutObjectAsync(putRequest, cancellationToken);

            photo.UriRemote = $"https://{_bucketName}.s3.amazonaws.com/{key}";
            photo.Uploaded = true;
            photo.UpdatedAt = DateTime.UtcNow;

            context.Photos.Update(photo);
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            // Log error but don't fail the operation
            // Photo will be synced later
            Console.WriteLine($"Failed to upload photo to S3: {ex.Message}");
        }

        return new PhotoDto
        {
            Id = photo.Id,
            EntityType = photo.EntityType,
            EntityId = photo.EntityId,
            UriLocal = photo.UriLocal,
            UriRemote = photo.UriRemote,
            Uploaded = photo.Uploaded,
            Description = photo.Description,
            CreatedAt = photo.CreatedAt,
        };
    }

    private static string GetContentType(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            _ => "application/octet-stream",
        };
    }
}
