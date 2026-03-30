using AgroLink.Application.Common.Exceptions;
using AgroLink.Application.Features.AnimalBrands.DTOs;
using AgroLink.Application.Interfaces;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using MediatR;

namespace AgroLink.Application.Features.AnimalBrands.Commands.Add;

public record AddAnimalBrandCommand(
    int FarmId,
    int AnimalId,
    int OwnerBrandId,
    DateTime? AppliedAt,
    string? Notes
) : IRequest<AnimalBrandDto>;

public class AddAnimalBrandCommandHandler(
    IAnimalRepository animalRepository,
    IOwnerBrandRepository ownerBrandRepository,
    IAnimalBrandRepository animalBrandRepository,
    IStorageService storageService,
    IUnitOfWork unitOfWork
) : IRequestHandler<AddAnimalBrandCommand, AnimalBrandDto>
{
    public async Task<AnimalBrandDto> Handle(
        AddAnimalBrandCommand request,
        CancellationToken cancellationToken
    )
    {
        var animal = await animalRepository.GetByIdInFarmAsync(request.AnimalId, request.FarmId);
        if (animal is null)
        {
            throw new NotFoundException(
                $"Animal with ID {request.AnimalId} not found in farm {request.FarmId}."
            );
        }

        var ownerBrand = await ownerBrandRepository.FirstOrDefaultAsync(ob =>
            ob.Id == request.OwnerBrandId && ob.Owner.FarmId == request.FarmId
        );
        if (ownerBrand is null)
        {
            throw new NotFoundException(
                $"OwnerBrand with ID {request.OwnerBrandId} not found in farm {request.FarmId}."
            );
        }

        var animalBrand = new AnimalBrand
        {
            AnimalId = request.AnimalId,
            OwnerBrandId = request.OwnerBrandId,
            AppliedAt = request.AppliedAt,
            Notes = request.Notes,
            CreatedAt = DateTime.UtcNow,
        };

        await animalBrandRepository.AddAsync(animalBrand);
        await unitOfWork.SaveChangesAsync();

        animalBrand.OwnerBrand = ownerBrand;
        return animalBrand.ToDto(storageService);
    }
}
