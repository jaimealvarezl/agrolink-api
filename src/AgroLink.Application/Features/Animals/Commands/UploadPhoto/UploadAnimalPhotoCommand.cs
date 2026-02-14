using AgroLink.Application.Common.Exceptions;
using AgroLink.Application.Features.Animals.DTOs;
using AgroLink.Application.Interfaces;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AgroLink.Application.Features.Animals.Commands.UploadPhoto;

public record UploadAnimalPhotoCommand(
    int AnimalId,
    Stream FileStream,
    string FileName,
    string ContentType,
    long Size,
    string? Description = null
) : IRequest<AnimalPhotoDto>;

public class UploadAnimalPhotoCommandHandler(
    IAnimalRepository animalRepository,
    IAnimalPhotoRepository animalPhotoRepository,
    IFarmMemberRepository farmMemberRepository,
    IStorageService storageService,
    IStoragePathProvider pathProvider,
    ICurrentUserService currentUserService,
    IUnitOfWork unitOfWork,
    ILogger<UploadAnimalPhotoCommandHandler> logger
) : IRequestHandler<UploadAnimalPhotoCommand, AnimalPhotoDto>
{
    private static readonly string[] AllowedExtensions = [".jpg", ".jpeg", ".png", ".webp"];
    private static readonly string[] AllowedMimeTypes = ["image/jpeg", "image/png", "image/webp"];

    public async Task<AnimalPhotoDto> Handle(
        UploadAnimalPhotoCommand request,
        CancellationToken cancellationToken
    )
    {
        using var scope = logger.BeginScope(
            new Dictionary<string, object> { ["AnimalId"] = request.AnimalId }
        );

        logger.LogInformation(
            "Starting photo upload for animal {AnimalId}. File: {FileName}, ContentType: {ContentType}, Size: {Size}",
            request.AnimalId,
            request.FileName,
            request.ContentType,
            request.Size
        );

        // 1. Validate File Extension and Content Type
        var extension = Path.GetExtension(request.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension))
        {
            logger.LogWarning("Invalid file extension: {Extension}", extension);
            throw new ArgumentException(
                $"File extension {extension} is not allowed. Allowed: {string.Join(", ", AllowedExtensions)}"
            );
        }

        if (!AllowedMimeTypes.Contains(request.ContentType.ToLowerInvariant()))
        {
            logger.LogWarning("Invalid content type: {ContentType}", request.ContentType);
            throw new ArgumentException(
                $"Content type {request.ContentType} is not allowed. Allowed: {string.Join(", ", AllowedMimeTypes)}"
            );
        }

        // 2. Validate File Signature (Magic Numbers)
        if (request.FileStream.CanSeek)
        {
            if (request.Size < 12)
            {
                logger.LogWarning("File too small: {Size} bytes", request.Size);
                throw new ArgumentException("File is too small to be a valid image.");
            }

            var buffer = new byte[12];
            var initialPos = request.FileStream.Position;
            if (initialPos != 0)
            {
                request.FileStream.Position = 0;
            }

            await request.FileStream.ReadExactlyAsync(buffer, 0, 12, cancellationToken);
            request.FileStream.Position = 0; // Reset for subsequent operations

            // JPEG: FF D8 FF
            var isJpeg = buffer[0] == 0xFF && buffer[1] == 0xD8 && buffer[2] == 0xFF;
            // PNG: 89 50 4E 47 0D 0A 1A 0A
            var isPng =
                buffer[0] == 0x89
                && buffer[1] == 0x50
                && buffer[2] == 0x4E
                && buffer[3] == 0x47
                && buffer[4] == 0x0D
                && buffer[5] == 0x0A
                && buffer[6] == 0x1A
                && buffer[7] == 0x0A;
            // WebP: RIFF ... WEBP
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
                logger.LogWarning(
                    "File signature mismatch. Filename: {FileName}, ContentType: {ContentType}, Header: {Header}",
                    request.FileName,
                    request.ContentType,
                    BitConverter.ToString(buffer)
                );
                throw new ArgumentException(
                    "File content does not match the expected image format (JPEG, PNG, WebP). The file may be corrupted."
                );
            }
        }

        var animal = await animalRepository.GetAnimalDetailsAsync(request.AnimalId);
        if (animal == null)
        {
            logger.LogWarning("Animal {AnimalId} not found", request.AnimalId);
            throw new ArgumentException($"Animal with ID {request.AnimalId} not found.");
        }

        if (animal.Lot?.Paddock == null)
        {
            logger.LogError(
                "Animal {AnimalId} is not correctly assigned to a lot/paddock. Lot: {LotId}",
                request.AnimalId,
                animal.LotId
            );
            throw new InvalidOperationException("Animal is not assigned to a valid paddock/farm.");
        }

        var farmId = animal.Lot.Paddock.FarmId;
        var userId = currentUserService.GetRequiredUserId();

        using var farmScope = logger.BeginScope(
            new Dictionary<string, object> { ["FarmId"] = farmId, ["UserId"] = userId }
        );

        logger.LogInformation(
            "Checking permissions for user {UserId} on farm {FarmId}",
            userId,
            farmId
        );
        var isMember = await farmMemberRepository.ExistsAsync(fm =>
            fm.FarmId == farmId && fm.UserId == userId
        );

        if (!isMember)
        {
            logger.LogWarning(
                "User {UserId} does not have permission for farm {FarmId}",
                userId,
                farmId
            );
            throw new ForbiddenAccessException("User does not have permission for this Farm.");
        }

        // Check if it's the first photo
        var isFirstPhoto = !await animalPhotoRepository.HasPhotosAsync(request.AnimalId);
        logger.LogInformation("Is first photo: {IsFirstPhoto}", isFirstPhoto);

        var animalPhoto = new AnimalPhoto
        {
            AnimalId = request.AnimalId,
            ContentType = request.ContentType,
            Size = request.Size,
            Description = request.Description,
            IsProfile = isFirstPhoto,
            UriRemote = "PENDING", // Temporary
            StorageKey = "PENDING", // Temporary
            UploadedAt = DateTime.UtcNow,
        };

        await animalPhotoRepository.AddAsync(animalPhoto);
        await unitOfWork.SaveChangesAsync();
        logger.LogInformation(
            "Database record created with temporary state. PhotoId: {PhotoId}",
            animalPhoto.Id
        );

        // Generate S3 Key
        var key = pathProvider.GetAnimalPhotoPath(
            farmId,
            request.AnimalId,
            animalPhoto.Id,
            request.FileName
        );
        logger.LogInformation("Generated storage key: {Key}", key);

        try
        {
            logger.LogInformation("Uploading file to storage...");
            await storageService.UploadFileAsync(
                key,
                request.FileStream,
                request.ContentType,
                request.Size
            );

            animalPhoto.UriRemote = storageService.GetFileUrl(key);
            animalPhoto.StorageKey = key;
            await unitOfWork.SaveChangesAsync();

            logger.LogInformation(
                "Photo upload and database update completed successfully. URL: {Url}",
                animalPhoto.UriRemote
            );
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to upload photo for animal {AnimalId} to storage. Key: {Key}",
                request.AnimalId,
                key
            );

            // Cleanup DB record if upload failure to maintain consistency
            logger.LogInformation(
                "Cleaning up database record {PhotoId} due to upload failure",
                animalPhoto.Id
            );
            animalPhotoRepository.Remove(animalPhoto);
            await unitOfWork.SaveChangesAsync();

            throw new InvalidOperationException(
                $"Failed to upload photo to storage: {ex.Message}",
                ex
            );
        }

        return new AnimalPhotoDto
        {
            Id = animalPhoto.Id,
            AnimalId = animalPhoto.AnimalId,
            UriRemote = storageService.GetPresignedUrl(
                animalPhoto.StorageKey,
                TimeSpan.FromHours(1)
            ),
            IsProfile = animalPhoto.IsProfile,
            ContentType = animalPhoto.ContentType,
            Size = animalPhoto.Size,
            Description = animalPhoto.Description,
            UploadedAt = animalPhoto.UploadedAt,
            CreatedAt = animalPhoto.CreatedAt,
        };
    }
}
