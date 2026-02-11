using AgroLink.Application.Common.Exceptions;
using AgroLink.Application.Interfaces;
using AgroLink.Domain.Interfaces;
using MediatR;

namespace AgroLink.Application.Features.Animals.Commands.DeletePhoto;

public record DeleteAnimalPhotoCommand(int AnimalId, int PhotoId) : IRequest<Unit>;

public class DeleteAnimalPhotoCommandHandler(
    IAnimalRepository animalRepository,
    IAnimalPhotoRepository animalPhotoRepository,
    IFarmMemberRepository farmMemberRepository,
    IStorageService storageService,
    ICurrentUserService currentUserService,
    IUnitOfWork unitOfWork
) : IRequestHandler<DeleteAnimalPhotoCommand, Unit>
{
    public async Task<Unit> Handle(
        DeleteAnimalPhotoCommand request,
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
                Console.WriteLine($"Failed to delete file from storage: {ex.Message}");
            }
        }

        animalPhotoRepository.Remove(photo);
        await unitOfWork.SaveChangesAsync();

        // If the deleted photo was the profile photo, and there are other photos,
        // we might want to set the first remaining one as profile.
        if (photo.IsProfile)
        {
            var remainingPhotos = await animalPhotoRepository.GetByAnimalIdAsync(request.AnimalId);
            var firstRemaining = remainingPhotos.FirstOrDefault();
            if (firstRemaining != null)
            {
                await animalPhotoRepository.SetProfilePhotoAsync(
                    request.AnimalId,
                    firstRemaining.Id
                );
                await unitOfWork.SaveChangesAsync();
            }
        }

        return Unit.Value;
    }
}
