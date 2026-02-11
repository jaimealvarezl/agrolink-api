using AgroLink.Application.Common.Exceptions;
using AgroLink.Application.Features.Animals.DTOs;
using AgroLink.Application.Interfaces;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using MediatR;

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
    IUnitOfWork unitOfWork
) : IRequestHandler<UploadAnimalPhotoCommand, AnimalPhotoDto>
{
    public async Task<AnimalPhotoDto> Handle(
        UploadAnimalPhotoCommand request,
        CancellationToken cancellationToken
    )
    {
        var animal = await animalRepository.GetAnimalDetailsAsync(request.AnimalId);
        if (animal == null)
        {
            throw new ArgumentException($"Animal with ID {request.AnimalId} not found.");
        }

        var farmId = animal.Lot.Paddock.FarmId;
        var userId = currentUserService.GetRequiredUserId();

        var isMember = await farmMemberRepository.ExistsAsync(fm =>
            fm.FarmId == farmId && fm.UserId == userId
        );

        if (!isMember)
        {
            throw new ForbiddenAccessException("User does not have permission for this Farm.");
        }

        // Check if it's the first photo
        var isFirstPhoto = !await animalPhotoRepository.HasPhotosAsync(request.AnimalId);

        var animalPhoto = new AnimalPhoto
        {
            AnimalId = request.AnimalId,
            ContentType = request.ContentType,
            Size = request.Size,
            Description = request.Description,
            IsProfile = isFirstPhoto,
            UriRemote = "PENDING", // Temporary
            UploadedAt = DateTime.UtcNow,
        };

        await animalPhotoRepository.AddAsync(animalPhoto);
        await unitOfWork.SaveChangesAsync();

        // Generate S3 Key
        var key = pathProvider.GetAnimalPhotoPath(
            farmId,
            request.AnimalId,
            animalPhoto.Id,
            request.FileName
        );

        try
        {
            await storageService.UploadFileAsync(key, request.FileStream, request.ContentType);
            animalPhoto.UriRemote = storageService.GetFileUrl(key);
            await unitOfWork.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            // If upload fails, we should probably remove the DB record or mark it as failed.
            // For now, let's throw to ensure the user knows it failed.
            // Cleanup:
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
            UriRemote = animalPhoto.UriRemote,
            IsProfile = animalPhoto.IsProfile,
            ContentType = animalPhoto.ContentType,
            Size = animalPhoto.Size,
            Description = animalPhoto.Description,
            UploadedAt = animalPhoto.UploadedAt,
            CreatedAt = animalPhoto.CreatedAt,
        };
    }
}
