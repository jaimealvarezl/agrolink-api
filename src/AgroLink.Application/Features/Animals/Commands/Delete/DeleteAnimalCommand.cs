using AgroLink.Application.Common.Exceptions;
using AgroLink.Application.Interfaces;
using AgroLink.Domain.Enums;
using AgroLink.Domain.Interfaces;
using MediatR;

namespace AgroLink.Application.Features.Animals.Commands.Delete;

public record DeleteAnimalCommand(int Id, int UserId) : IRequest;

public class DeleteAnimalCommandHandler(
    IAnimalRepository animalRepository,
    ICurrentUserService currentUserService,
    IUnitOfWork unitOfWork
) : IRequestHandler<DeleteAnimalCommand>
{
    public async Task Handle(DeleteAnimalCommand request, CancellationToken cancellationToken)
    {
        var animal = await animalRepository.GetByIdAsync(request.Id, request.UserId);
        if (animal == null)
        {
            throw new ArgumentException("Animal not found or access denied.");
        }

        // Security check: ensure animal belongs to the current farm context
        if (
            currentUserService.CurrentFarmId.HasValue
            && animal.Lot?.Paddock?.FarmId != currentUserService.CurrentFarmId.Value
        )
        {
            throw new ForbiddenAccessException("You do not have access to this animal");
        }

        // Soft delete via LifeStatus
        animal.LifeStatus = LifeStatus.Deleted;
        animal.UpdatedAt = DateTime.UtcNow;

        animalRepository.Update(animal);
        await unitOfWork.SaveChangesAsync();
    }
}
