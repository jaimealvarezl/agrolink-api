using AgroLink.Application.Common.Exceptions;
using AgroLink.Domain.Interfaces;
using MediatR;

namespace AgroLink.Application.Features.AnimalBrands.Commands.Remove;

public record RemoveAnimalBrandCommand(int FarmId, int AnimalId, int AnimalBrandId) : IRequest;

public class RemoveAnimalBrandCommandHandler(
    IAnimalRepository animalRepository,
    IAnimalBrandRepository animalBrandRepository,
    IUnitOfWork unitOfWork
) : IRequestHandler<RemoveAnimalBrandCommand>
{
    public async Task Handle(RemoveAnimalBrandCommand request, CancellationToken cancellationToken)
    {
        var animal = await animalRepository.GetByIdInFarmAsync(
            request.AnimalId,
            request.FarmId,
            cancellationToken
        );
        if (animal is null)
        {
            throw new NotFoundException(
                $"Animal with ID {request.AnimalId} not found in farm {request.FarmId}."
            );
        }

        var animalBrand = await animalBrandRepository.FirstOrDefaultAsync(
            ab => ab.Id == request.AnimalBrandId && ab.AnimalId == request.AnimalId,
            cancellationToken
        );
        if (animalBrand is null)
        {
            throw new NotFoundException(
                $"AnimalBrand with ID {request.AnimalBrandId} not found for animal {request.AnimalId}."
            );
        }

        animalBrandRepository.Remove(animalBrand);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
