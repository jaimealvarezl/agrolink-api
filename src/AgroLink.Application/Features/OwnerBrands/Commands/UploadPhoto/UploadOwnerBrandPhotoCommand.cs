using AgroLink.Application.Common.Exceptions;
using AgroLink.Application.Features.OwnerBrands.DTOs;
using AgroLink.Application.Interfaces;
using AgroLink.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AgroLink.Application.Features.OwnerBrands.Commands.UploadPhoto;

public record UploadOwnerBrandPhotoCommand(
    int FarmId,
    int OwnerId,
    int BrandId,
    Stream FileStream,
    string FileName,
    string ContentType,
    long Size
) : IRequest<OwnerBrandDto>;

public class UploadOwnerBrandPhotoCommandHandler(
    IOwnerBrandRepository ownerBrandRepository,
    IOwnerRepository ownerRepository,
    IStorageService storageService,
    IStoragePathProvider pathProvider,
    IUnitOfWork unitOfWork,
    ILogger<UploadOwnerBrandPhotoCommandHandler> logger
) : IRequestHandler<UploadOwnerBrandPhotoCommand, OwnerBrandDto>
{
    private static readonly string[] _allowedExtensions = [".jpg", ".jpeg", ".png", ".webp"];
    private static readonly string[] _allowedMimeTypes = ["image/jpeg", "image/png", "image/webp"];

    public async Task<OwnerBrandDto> Handle(
        UploadOwnerBrandPhotoCommand request,
        CancellationToken cancellationToken
    )
    {
        var extension = Path.GetExtension(request.FileName).ToLowerInvariant();
        if (!_allowedExtensions.Contains(extension))
        {
            throw new ArgumentException(
                $"File extension {extension} is not allowed. Allowed: {string.Join(", ", _allowedExtensions)}"
            );
        }

        if (!_allowedMimeTypes.Contains(request.ContentType.ToLowerInvariant()))
        {
            throw new ArgumentException(
                $"Content type {request.ContentType} is not allowed. Allowed: {string.Join(", ", _allowedMimeTypes)}"
            );
        }

        if (request.FileStream.CanSeek)
        {
            if (request.Size < 12)
            {
                throw new ArgumentException("File is too small to be a valid image.");
            }

            var buffer = new byte[12];
            if (request.FileStream.Position != 0)
            {
                request.FileStream.Position = 0;
            }

            await request.FileStream.ReadExactlyAsync(buffer, 0, 12, cancellationToken);
            request.FileStream.Position = 0;

            var isJpeg = buffer[0] == 0xFF && buffer[1] == 0xD8 && buffer[2] == 0xFF;
            var isPng =
                buffer[0] == 0x89
                && buffer[1] == 0x50
                && buffer[2] == 0x4E
                && buffer[3] == 0x47
                && buffer[4] == 0x0D
                && buffer[5] == 0x0A
                && buffer[6] == 0x1A
                && buffer[7] == 0x0A;
            var isWebp =
                buffer[0] == 0x52
                && buffer[1] == 0x49
                && buffer[2] == 0x46
                && buffer[3] == 0x46
                && buffer[8] == 0x57
                && buffer[9] == 0x45
                && buffer[10] == 0x42
                && buffer[11] == 0x50;

            if (!isJpeg && !isPng && !isWebp)
            {
                throw new ArgumentException(
                    "File content does not match the expected image format (JPEG, PNG, WebP). The file may be corrupted."
                );
            }
        }

        var ownerExists = await ownerRepository.ExistsAsync(o =>
            o.Id == request.OwnerId && o.FarmId == request.FarmId
        );
        if (!ownerExists)
        {
            throw new NotFoundException(
                $"Owner with ID {request.OwnerId} not found in farm {request.FarmId}."
            );
        }

        var brand = await ownerBrandRepository.FirstOrDefaultAsync(b =>
            b.Id == request.BrandId && b.OwnerId == request.OwnerId
        );
        if (brand == null)
        {
            throw new NotFoundException(
                $"Brand with ID {request.BrandId} not found for owner {request.OwnerId}."
            );
        }

        var oldStorageKey = brand.PhotoStorageKey;
        var newKey = pathProvider.GetOwnerBrandPhotoPath(
            request.FarmId,
            request.BrandId,
            request.FileName
        );

        try
        {
            await storageService.UploadFileAsync(
                newKey,
                request.FileStream,
                request.ContentType,
                request.Size
            );

            brand.PhotoUrl = storageService.GetFileUrl(newKey);
            brand.PhotoStorageKey = newKey;
            brand.UpdatedAt = DateTime.UtcNow;

            ownerBrandRepository.Update(brand);
            await unitOfWork.SaveChangesAsync();

            // Delete old photo after successful upload and DB update
            if (oldStorageKey != null)
            {
                try
                {
                    await storageService.DeleteFileAsync(oldStorageKey);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(
                        ex,
                        "Failed to delete old brand photo. Key: {Key}",
                        oldStorageKey
                    );
                }
            }
        }
        catch (Exception ex) when (ex is not NotFoundException && ex is not ArgumentException)
        {
            logger.LogError(
                ex,
                "Failed to upload brand photo for brand {BrandId}. Key: {Key}",
                request.BrandId,
                newKey
            );
            throw new InvalidOperationException(
                $"Failed to upload photo to storage: {ex.Message}",
                ex
            );
        }

        return new OwnerBrandDto
        {
            Id = brand.Id,
            OwnerId = brand.OwnerId,
            Description = brand.Description,
            PhotoUrl = storageService.GetPresignedUrl(
                brand.PhotoStorageKey!,
                TimeSpan.FromHours(1)
            ),
            IsActive = brand.IsActive,
            CreatedAt = brand.CreatedAt,
            UpdatedAt = brand.UpdatedAt,
        };
    }
}
