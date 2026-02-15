using AgroLink.Domain.Enums;
using AgroLink.Domain.Interfaces;
using MediatR;

namespace AgroLink.Application.Features.Animals.Commands.Delete;

public record DeleteAnimalCommand(int Id, int UserId) : IRequest;

public class DeleteAnimalCommandHandler(IAnimalRepository animalRepository, IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteAnimalCommand>
{
    public async Task Handle(DeleteAnimalCommand request, CancellationToken cancellationToken)
    {
        var animal = await animalRepository.GetByIdAsync(request.Id, request.UserId);
        if (animal == null)
        {
            throw new ArgumentException("Animal not found or access denied.");
        }

        // Soft delete via LifeStatus
        animal.LifeStatus = LifeStatus.Deleted;
        animal.UpdatedAt = DateTime.UtcNow;

        animalRepository.Update(animal);
        await unitOfWork.SaveChangesAsync();
    }
}
