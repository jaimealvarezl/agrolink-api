using AgroLink.Application.Common.Exceptions;
using AgroLink.Application.Features.OwnerBrands.DTOs;
using AgroLink.Application.Interfaces;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using MediatR;

namespace AgroLink.Application.Features.OwnerBrands.Commands.Create;

public record CreateOwnerBrandCommand(int FarmId, int OwnerId, string Description)
    : IRequest<OwnerBrandDto>;

public class CreateOwnerBrandCommandHandler(
    IOwnerBrandRepository ownerBrandRepository,
    IOwnerRepository ownerRepository,
    IStorageService storageService,
    IUnitOfWork unitOfWork
) : IRequestHandler<CreateOwnerBrandCommand, OwnerBrandDto>
{
    public async Task<OwnerBrandDto> Handle(
        CreateOwnerBrandCommand request,
        CancellationToken cancellationToken
    )
    {
        var ownerExists = await ownerRepository.ExistsAsync(o =>
            o.Id == request.OwnerId && o.FarmId == request.FarmId
        );
        if (!ownerExists)
        {
            throw new NotFoundException(
                $"Owner with ID {request.OwnerId} not found in farm {request.FarmId}."
            );
        }

        var brand = new OwnerBrand
        {
            OwnerId = request.OwnerId,
            Description = request.Description,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        };

        await ownerBrandRepository.AddAsync(brand);
        await unitOfWork.SaveChangesAsync();

        return brand.ToDto(storageService);
    }
}
