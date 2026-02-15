using AgroLink.Application.Interfaces;
using AgroLink.Domain.Interfaces;
using MediatR;

namespace AgroLink.Application.Features.Animals.Commands.SetProfilePhoto;

public record SetAnimalProfilePhotoCommand(int AnimalId, int PhotoId, int UserId) : IRequest<Unit>;

public class SetAnimalProfilePhotoCommandHandler(
    IAnimalRepository animalRepository,
    IAnimalPhotoRepository animalPhotoRepository,
    IUnitOfWork unitOfWork
) : IRequestHandler<SetAnimalProfilePhotoCommand, Unit>
{
    public async Task<Unit> Handle(
        SetAnimalProfilePhotoCommand request,
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

        await animalPhotoRepository.SetProfilePhotoAsync(request.AnimalId, request.PhotoId);
        await unitOfWork.SaveChangesAsync();

        return Unit.Value;
    }
}
