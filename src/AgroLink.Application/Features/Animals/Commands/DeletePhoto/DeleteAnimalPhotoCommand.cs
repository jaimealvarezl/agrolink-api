using AgroLink.Application.Interfaces;
using AgroLink.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AgroLink.Application.Features.Animals.Commands.DeletePhoto;

public record DeleteAnimalPhotoCommand(int AnimalId, int PhotoId, int UserId) : IRequest<Unit>;

public class DeleteAnimalPhotoCommandHandler(
    IAnimalRepository animalRepository,
    IAnimalPhotoRepository animalPhotoRepository,
    IStorageService storageService,
    IUnitOfWork unitOfWork,
    ILogger<DeleteAnimalPhotoCommandHandler> logger
) : IRequestHandler<DeleteAnimalPhotoCommand, Unit>
{
    public async Task<Unit> Handle(
        DeleteAnimalPhotoCommand request,
        CancellationToken cancellationToken
    )
    {
        var animal = await animalRepository.GetAnimalDetailsAsync(request.AnimalId, request.UserId);
        if (animal == null)
        {
            throw new ArgumentException(
                $"Animal with ID {request.AnimalId} not found or access denied."
            );
        }

        var photo = await animalPhotoRepository.GetByIdAsync(request.PhotoId);
        if (photo == null || photo.AnimalId != request.AnimalId)
        {
            throw new ArgumentException("Photo not found or does not belong to the animal.");
        }

        // Delete from Storage
        if (!string.IsNullOrEmpty(photo.StorageKey))
        {
            try
            {
                await storageService.DeleteFileAsync(photo.StorageKey);
            }
            catch (Exception ex)
            {
                // Log and continue, as storage failure shouldn't necessarily block DB deletion
                // but we should be careful.
                logger.LogError(
                    ex,
                    "Failed to delete file from storage for photo {PhotoId}",
                    photo.Id
                );
            }
        }

        animalPhotoRepository.Remove(photo);

        // If the deleted photo was the profile photo, and there are other photos,
        // we might want to set the first remaining one as profile.
        if (photo.IsProfile)
        {
            var remainingPhotos = await animalPhotoRepository.GetByAnimalIdAsync(request.AnimalId);
            var firstRemaining = remainingPhotos
                .OrderByDescending(p => p.UploadedAt)
                .FirstOrDefault(p => p.Id != request.PhotoId);

            if (firstRemaining != null)
            {
                await animalPhotoRepository.SetProfilePhotoAsync(
                    request.AnimalId,
                    firstRemaining.Id
                );
            }
        }

        await unitOfWork.SaveChangesAsync();

        return Unit.Value;
    }
}
