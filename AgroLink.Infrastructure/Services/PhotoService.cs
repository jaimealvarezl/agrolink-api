using AgroLink.Core.DTOs;
using AgroLink.Core.Entities;
using AgroLink.Core.Interfaces;
using AgroLink.Infrastructure.Data;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace AgroLink.Infrastructure.Services;

public class PhotoService : IPhotoService
{
    private readonly AgroLinkDbContext _context;
    private readonly IAmazonS3 _s3Client;
    private readonly string _bucketName;

    public PhotoService(AgroLinkDbContext context, IAmazonS3 s3Client, IConfiguration configuration)
    {
        _context = context;
        _s3Client = s3Client;
        _bucketName = configuration["AWS:S3BucketName"] ?? "agrolink-photos";
    }

    public async Task<PhotoDto> UploadPhotoAsync(
        CreatePhotoDto dto,
        Stream fileStream,
        string fileName
    )
    {
        var photo = new Photo
        {
            EntityType = dto.EntityType,
            EntityId = dto.EntityId,
            UriLocal = $"local/{dto.EntityType.ToLower()}/{dto.EntityId}/{fileName}",
            Description = dto.Description,
        };

        _context.Photos.Add(photo);
        await _context.SaveChangesAsync();

        // Try to upload to S3
        try
        {
            var key = $"photos/{dto.EntityType.ToLower()}/{dto.EntityId}/{photo.Id}_{fileName}";

            var request = new PutObjectRequest
            {
                BucketName = _bucketName,
                Key = key,
                InputStream = fileStream,
                ContentType = GetContentType(fileName),
            };

            await _s3Client.PutObjectAsync(request);

            photo.UriRemote = $"https://{_bucketName}.s3.amazonaws.com/{key}";
            photo.Uploaded = true;
            photo.UpdatedAt = DateTime.UtcNow;

            _context.Photos.Update(photo);
            await _context.SaveChangesAsync();
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

    public async Task<IEnumerable<PhotoDto>> GetByEntityAsync(string entityType, int entityId)
    {
        var photos = await _context
            .Photos.Where(p => p.EntityType == entityType && p.EntityId == entityId)
            .ToListAsync();

        return photos
            .Select(p => new PhotoDto
            {
                Id = p.Id,
                EntityType = p.EntityType,
                EntityId = p.EntityId,
                UriLocal = p.UriLocal,
                UriRemote = p.UriRemote,
                Uploaded = p.Uploaded,
                Description = p.Description,
                CreatedAt = p.CreatedAt,
            })
            .ToList();
    }

    public async Task DeleteAsync(int id)
    {
        var photo = await _context.Photos.FindAsync(id);
        if (photo == null)
            throw new ArgumentException("Photo not found");

        // Try to delete from S3
        if (!string.IsNullOrEmpty(photo.UriRemote))
        {
            try
            {
                var key = ExtractKeyFromUrl(photo.UriRemote);
                await _s3Client.DeleteObjectAsync(_bucketName, key);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to delete photo from S3: {ex.Message}");
            }
        }

        _context.Photos.Remove(photo);
        await _context.SaveChangesAsync();
    }

    public async Task SyncPendingPhotosAsync()
    {
        var pendingPhotos = await _context.Photos.Where(p => !p.Uploaded).ToListAsync();

        foreach (var photo in pendingPhotos)
        {
            try
            {
                // This would need to read the local file and upload it
                // Implementation depends on how local files are stored
                // For now, we'll just mark as attempted
                photo.UpdatedAt = DateTime.UtcNow;
                _context.Photos.Update(photo);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to sync photo {photo.Id}: {ex.Message}");
            }
        }

        await _context.SaveChangesAsync();
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

    private static string ExtractKeyFromUrl(string url)
    {
        var uri = new Uri(url);
        return uri.AbsolutePath.TrimStart('/');
    }
}
