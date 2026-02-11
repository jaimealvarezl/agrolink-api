using AgroLink.Application.Features.Photos.DTOs;
using AgroLink.Application.Interfaces;
using AgroLink.Domain.Entities;
using MediatR;

namespace AgroLink.Application.Features.Photos.Commands.UploadPhoto;

public class UploadPhotoCommandHandler(
    IPhotoRepository photoRepository,
    IStorageService storageService
) : IRequestHandler<UploadPhotoCommand, PhotoDto>
{
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

        await photoRepository.AddPhotoAsync(photo);

        // Try to upload to S3
        try
        {
            var key =
                $"photos/{request.PhotoDto.EntityType.ToLower()}/{request.PhotoDto.EntityId}/{photo.Id}_{request.FileName}";

            await storageService.UploadFileAsync(
                key,
                request.FileStream,
                GetContentType(request.FileName)
            );

            photo.UriRemote = storageService.GetFileUrl(key);
            photo.Uploaded = true;
            photo.UpdatedAt = DateTime.UtcNow;

            await photoRepository.UpdatePhotoAsync(photo);
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
