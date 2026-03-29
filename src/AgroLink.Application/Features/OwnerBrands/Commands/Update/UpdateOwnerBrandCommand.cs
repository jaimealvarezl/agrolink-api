using AgroLink.Application.Common.Exceptions;
using AgroLink.Application.Features.OwnerBrands.DTOs;
using AgroLink.Application.Interfaces;
using AgroLink.Domain.Interfaces;
using MediatR;

namespace AgroLink.Application.Features.OwnerBrands.Commands.Update;

public record UpdateOwnerBrandCommand(int FarmId, int OwnerId, int BrandId, string Description)
    : IRequest<OwnerBrandDto>;

public class UpdateOwnerBrandCommandHandler(
    IOwnerBrandRepository ownerBrandRepository,
    IOwnerRepository ownerRepository,
    IStorageService storageService,
    IUnitOfWork unitOfWork
) : IRequestHandler<UpdateOwnerBrandCommand, OwnerBrandDto>
{
    public async Task<OwnerBrandDto> Handle(
        UpdateOwnerBrandCommand request,
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

        var brand = await ownerBrandRepository.FirstOrDefaultAsync(b =>
            b.Id == request.BrandId && b.OwnerId == request.OwnerId
        );
        if (brand == null)
        {
            throw new NotFoundException(
                $"Brand with ID {request.BrandId} not found for owner {request.OwnerId}."
            );
        }

        brand.Description = request.Description;
        brand.UpdatedAt = DateTime.UtcNow;

        ownerBrandRepository.Update(brand);
        await unitOfWork.SaveChangesAsync();

        return brand.ToDto(storageService);
    }
}
