using AgroLink.Application.Interfaces;
using AgroLink.Domain.Enums;
using AgroLink.Domain.Interfaces;
using MediatR;

namespace AgroLink.Application.Features.Animals.Commands.Delete;

public record DeleteAnimalCommand(int Id) : IRequest;

public class DeleteAnimalCommandHandler(
    IAnimalRepository animalRepository,
    ILotRepository lotRepository,
    IFarmMemberRepository farmMemberRepository,
    ICurrentUserService currentUserService,
    IUnitOfWork unitOfWork
) : IRequestHandler<DeleteAnimalCommand>
{
    public async Task Handle(DeleteAnimalCommand request, CancellationToken cancellationToken)
    {
        var animal = await animalRepository.GetByIdAsync(request.Id);
        if (animal == null)
        {
            throw new ArgumentException("Animal not found");
        }

        // Validate permissions
        var lot = await lotRepository.GetLotWithPaddockAsync(animal.LotId);
        if (lot == null)
        {
            throw new InvalidOperationException("Animal's lot not found.");
        }

        var userId = currentUserService.GetRequiredUserId();
        var isMember = await farmMemberRepository.ExistsAsync(fm =>
            fm.FarmId == lot.Paddock.FarmId && fm.UserId == userId
        );
        if (!isMember)
        {
            throw new ArgumentException("User does not have permission for this Farm.");
        }

        // Soft delete via LifeStatus
        animal.LifeStatus = LifeStatus.Deleted;
        animal.UpdatedAt = DateTime.UtcNow;

        animalRepository.Update(animal);
        await unitOfWork.SaveChangesAsync();
    }
}
