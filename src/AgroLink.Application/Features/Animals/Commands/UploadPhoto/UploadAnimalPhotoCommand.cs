using AgroLink.Application.Common.Utilities;
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
    int UserId,
    string? Description = null
) : IRequest<AnimalPhotoDto>;

public class UploadAnimalPhotoCommandHandler(
    IAnimalRepository animalRepository,
    IAnimalPhotoRepository animalPhotoRepository,
    IStorageService storageService,
    IStoragePathProvider pathProvider,
    IUnitOfWork unitOfWork,
    ILogger<UploadAnimalPhotoCommandHandler> logger
) : IRequestHandler<UploadAnimalPhotoCommand, AnimalPhotoDto>
{
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

        var seekableStream = await ImageFileValidator.ValidateAsync(
            request.FileStream,
            request.FileName,
            request.ContentType,
            request.Size,
            cancellationToken
        );

        var animal = await animalRepository.GetAnimalDetailsAsync(request.AnimalId, request.UserId);
        if (animal == null)
        {
            logger.LogWarning("Animal {AnimalId} not found or access denied", request.AnimalId);
            throw new ArgumentException(
                $"Animal with ID {request.AnimalId} not found or access denied."
            );
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
                seekableStream,
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
