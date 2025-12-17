using AgroLink.Domain.Interfaces;
using MediatR;

namespace AgroLink.Application.Features.Animals.Commands.Delete;

public record DeleteAnimalCommand(int Id) : IRequest;

public class DeleteAnimalCommandHandler(IAnimalRepository animalRepository, IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteAnimalCommand>
{
    public async Task Handle(DeleteAnimalCommand request, CancellationToken cancellationToken)
    {
        var animal = await animalRepository.GetByIdAsync(request.Id);
        if (animal == null)
        {
            throw new ArgumentException("Animal not found");
        }

        animalRepository.Remove(animal);
        await unitOfWork.SaveChangesAsync();
    }
}
